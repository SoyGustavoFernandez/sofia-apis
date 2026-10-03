namespace SOFIA.Application.Dashboard.Common;

// Branches the dashboard figures cover, resolved once per request as an explicit list
public record DashboardScope(IReadOnlyList<DashboardSucursal> Sucursales, bool EmpresaCompleta)
{
    public List<Guid> Ids { get; } = [.. Sucursales.Select(s => s.Id)];

    public bool MultiSucursal => Sucursales.Count > 1;
}
