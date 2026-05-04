using Microsoft.EntityFrameworkCore;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Sucursal> Sucursales { get; }
    DbSet<Empleado> Empleados { get; }
    DbSet<UnidadMedida> UnidadesMedida { get; }
    DbSet<Laboratorio> Laboratorios { get; }
    DbSet<IngredienteActivo> IngredientesActivos { get; }
    DbSet<Medicamento> Medicamentos { get; }
    DbSet<JerarquiaUoM> JerarquiasUoM { get; }
    DbSet<FormulacionClinica> FormulacionesClinicas { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
