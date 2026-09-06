using SOFIA.Domain.Common;

namespace SOFIA.Domain.Entities;

public sealed class Rol : BaseEntity
{
    private Rol() { } // Required for EF Core

    public string NombreRol { get; private set; } = string.Empty;
    public string? Descripcion { get; private set; }
    public int NivelJerarquia { get; private set; }

    // Navigation Properties
    public ICollection<Cuenta> Cuentas { get; private set; } = [];
    public ICollection<PermisoRol> Permisos { get; private set; } = [];
    public ICollection<Sucursal> Sucursales { get; private set; } = [];

    public void AddSucursal(Sucursal sucursal)
    {
        if (!Sucursales.Any(s => s.Id == sucursal.Id))
        {
            Sucursales.Add(sucursal);
        }
    }

    public void RemoveSucursal(Guid sucursalId)
    {
        var sucursal = Sucursales.FirstOrDefault(s => s.Id == sucursalId);
        if (sucursal != null)
        {
            _ = Sucursales.Remove(sucursal);
        }
    }

    public static Result<Rol> Create(
        string nombreRol,
        string? descripcion,
        int nivelJerarquia = 0)
    {
        if (string.IsNullOrWhiteSpace(nombreRol))
        {
            return Result.Failure<Rol>(Error.Validation("Rol.Nombre", "Nombre de Rol is required."));
        }

        if (nombreRol.Length > 50)
        {
            return Result.Failure<Rol>(Error.Validation("Rol.Nombre", "Nombre de Rol must not exceed 50 characters."));
        }

        return Result.Success(new Rol
        {
            NombreRol = nombreRol,
            Descripcion = descripcion,
            NivelJerarquia = nivelJerarquia
        });
    }

    public void Update(string? descripcion, int nivelJerarquia)
    {
        Descripcion = descripcion;
        NivelJerarquia = nivelJerarquia;
    }
}
