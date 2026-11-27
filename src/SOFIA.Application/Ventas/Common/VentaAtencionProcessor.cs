using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Ventas.Commands.CreateVenta;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.Application.Ventas.Common;

public sealed record AtencionPreparada(CitaServicio Cita, ServicioCatalogo Servicio, VentaServicio Linea, AtencionDatos Datos);

public static class VentaAtencionProcessor
{
    // Resolves the service, its price and the appointment (a new one for a walk-in) before the sale is built
    public static async Task<Result<AtencionPreparada>> PrepararAsync(IApplicationDbContext context, CreateVentaCommand request, Guid sucursalId, CancellationToken cancellationToken)
    {
        var atencion = request.Atencion!;

        var servicio = await context.ServiciosCatalogo.FirstOrDefaultAsync(s => s.Id == atencion.ServicioId, cancellationToken);
        if (servicio is null)
        {
            return Result.Failure<AtencionPreparada>(Error.NotFound("ServicioCatalogo.NotFound", "The service does not exist."));
        }

        var datos = new AtencionDatos(atencion.Origen, atencion.LoteId, atencion.MedicamentoTraido, atencion.Dosis, atencion.Resultado, atencion.VentaPreviaId);
        var medicamento = await ValidarMedicamentoAsync(context, request, servicio, datos, cancellationToken);
        if (medicamento.IsFailure)
        {
            return Result.Failure<AtencionPreparada>(medicamento.Error);
        }

        var cita = await ResolverCitaAsync(context, request, sucursalId, cancellationToken);
        if (cita.IsFailure)
        {
            return Result.Failure<AtencionPreparada>(cita.Error);
        }

        var linea = VentaServicio.Create(servicio.Id, cita.Value.Id, servicio.Precio);
        return linea.IsSuccess
            ? Result.Success(new AtencionPreparada(cita.Value, servicio, linea.Value, datos))
            : Result.Failure<AtencionPreparada>(linea.Error);
    }

    // Medicine bought now must be a line of this sale; bought earlier must be in a completed sale of the same patient or of nobody
    private static async Task<Result> ValidarMedicamentoAsync(IApplicationDbContext context, CreateVentaCommand request, ServicioCatalogo servicio, AtencionDatos datos, CancellationToken cancellationToken)
    {
        if (!servicio.AplicaMedicamento)
        {
            return Result.Success();
        }

        if (datos.Origen == OrigenMedicamento.CompradoAhora && !request.Detalles.Exists(d => d.LoteId == datos.LoteId))
        {
            return Result.Failure(Error.Validation("Atencion.LoteNoVendido", "The applied lot must be one of the products of this sale."));
        }

        if (datos.Origen != OrigenMedicamento.CompradoAntes)
        {
            return Result.Success();
        }

        var existe = await context.Ventas.AnyAsync(
            v => v.Id == datos.VentaPreviaId
                && v.Estado == EstadoVenta.Completada
                && (v.ClienteId == null || v.ClienteId == request.ClienteId)
                && v.Detalles.Any(d => d.LoteId == datos.LoteId),
            cancellationToken);

        return existe
            ? Result.Success()
            : Result.Failure(Error.Validation("Atencion.VentaPrevia", "The earlier sale does not contain that medicine for this patient."));
    }

    private static async Task<Result<CitaServicio>> ResolverCitaAsync(IApplicationDbContext context, CreateVentaCommand request, Guid sucursalId, CancellationToken cancellationToken)
    {
        var atencion = request.Atencion!;
        if (atencion.CitaId is not { } citaId)
        {
            var nueva = CitaServicio.CrearSinCita(request.ClienteId, atencion.ServicioId, sucursalId, DateTimeOffset.UtcNow);
            if (nueva.IsSuccess)
            {
                _ = context.CitasServicio.Add(nueva.Value);
            }

            return nueva;
        }

        var cita = await context.CitasServicio.FirstOrDefaultAsync(c => c.Id == citaId && c.SucursalId == sucursalId, cancellationToken);
        if (cita is null)
        {
            return Result.Failure<CitaServicio>(Error.NotFound("CitaServicio.NotFound", "The appointment does not exist."));
        }

        return cita.PacienteId != request.ClienteId || cita.ServicioId != atencion.ServicioId
            ? Result.Failure<CitaServicio>(Error.Validation("Atencion.OtraCita", "The sale does not match the patient and service of the appointment."))
            : Result.Success(cita);
    }
}
