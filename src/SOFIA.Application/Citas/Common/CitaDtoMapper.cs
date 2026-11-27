using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Citas.Common;

public static class CitaDtoMapper
{
    // Related rows are read apart, deleted ones included, so an appointment never drops out of the list or the count
    public static async Task<List<CitaDto>> MapAsync(IApplicationDbContext context, IReadOnlyCollection<CitaServicio> citas, CancellationToken cancellationToken)
    {
        var pacienteIds = citas.Where(c => c.PacienteId.HasValue).Select(c => c.PacienteId!.Value).Distinct().ToList();
        var servicioIds = citas.Select(c => c.ServicioId).Distinct().ToList();
        var sucursalIds = citas.Select(c => c.SucursalId).Distinct().ToList();

        var pacientes = await context.Pacientes.IgnoreQueryFilters([QueryFilters.SoftDelete]).AsNoTracking()
            .Where(p => pacienteIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, cancellationToken);
        var servicios = await context.ServiciosCatalogo.IgnoreQueryFilters([QueryFilters.SoftDelete]).AsNoTracking()
            .Where(s => servicioIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id, cancellationToken);
        var sucursales = await context.Sucursales.IgnoreQueryFilters([QueryFilters.SoftDelete]).AsNoTracking()
            .Where(s => sucursalIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id, s => s.Nombre, cancellationToken);

        return [.. citas.Select(c =>
        {
            var paciente = c.PacienteId is { } pacienteId ? pacientes.GetValueOrDefault(pacienteId) : null;
            var servicio = servicios.GetValueOrDefault(c.ServicioId);
            return new CitaDto(
                c.Id,
                c.PacienteId,
                paciente?.NombreApellidos,
                paciente?.ContactoPrimario,
                c.ServicioId,
                servicio?.Nombre ?? "-",
                servicio?.Precio ?? 0m,
                sucursales.GetValueOrDefault(c.SucursalId) ?? "-",
                c.FechaHora,
                c.Estado,
                c.AceptaRecordatorio,
                servicio?.AplicaMedicamento ?? false,
                servicio?.RegistraResultado ?? false,
                c.Dosis,
                c.Resultado,
                c.VentaId);
        })];
    }
}
