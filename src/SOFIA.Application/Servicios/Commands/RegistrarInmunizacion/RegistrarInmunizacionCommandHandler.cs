using SOFIA.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Servicios.Commands.RegistrarInmunizacion;

public class RegistrarInmunizacionCommandHandler(IApplicationDbContext dbContext) : IRequestHandler<RegistrarInmunizacionCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(RegistrarInmunizacionCommand request, CancellationToken cancellationToken)
    {
        // Tenant-filtered lookups: an id from another company must not be stored as a reference
        if (!await dbContext.Pacientes.AnyAsync(p => p.Id == request.ClienteId, cancellationToken))
        {
            return Result.Failure<Guid>(Error.NotFound("Paciente.NotFound", "The specified patient does not exist."), 404);
        }

        if (!await dbContext.Empleados.AnyAsync(e => e.Id == request.ProfesionalAdmnId, cancellationToken))
        {
            return Result.Failure<Guid>(Error.NotFound("Empleado.NotFound", "El empleado especificado no existe."), 404);
        }

        if (!await dbContext.Medicamentos.AnyAsync(m => m.Id == request.ProductoId, cancellationToken))
        {
            return Result.Failure<Guid>(Error.NotFound("Medicamento.NotFound", "The specified product does not exist."), 404);
        }

        if (!await dbContext.LotesInventario.AnyAsync(l => l.Id == request.LoteId && l.ProductoId == request.ProductoId, cancellationToken))
        {
            return Result.Failure<Guid>(Error.NotFound("LoteInventario.NotFound", "The specified batch does not exist."), 404);
        }

        if (request.VentaId is { } ventaId && !await dbContext.Ventas.AnyAsync(v => v.Id == ventaId, cancellationToken))
        {
            return Result.Failure<Guid>(Error.NotFound("Venta.NotFound", "La venta original no fue encontrada."), 404);
        }

        var createResult = Domain.Entities.ServicioClinicoInmunizacion.Create(request.VentaId, request.ClienteId, request.ProfesionalAdmnId, request.ProductoId, request.LoteId, request.ViaAdministracion, request.SitioAnatomico, request.VolumenDosis, request.FechaAdmnFisica, request.FechaEntregaVis, request.ModalidadRegistro);
        if (createResult.IsFailure)
        {
            return Result.Failure<Guid>(createResult.Error);
        }

        var entity = createResult.Value!;
        _ = dbContext.ServiciosClinicosInmunizacion.Add(entity);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(entity.Id);

    }
}
