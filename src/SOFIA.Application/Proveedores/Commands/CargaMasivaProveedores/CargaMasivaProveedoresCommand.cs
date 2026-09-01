using SOFIA.Application.Common.Interfaces;

namespace SOFIA.Application.Proveedores.Commands.CargaMasivaProveedores;

public record ProveedorImportRow(
    string RazonSocial,
    string TaxId,
    string? TerminosFinancieros,
    decimal? CalificacionEsg,
    decimal TasaCumplimiento);

public record CargaMasivaProveedoresCommand(List<ProveedorImportRow> Rows) : ICommand<int>;
