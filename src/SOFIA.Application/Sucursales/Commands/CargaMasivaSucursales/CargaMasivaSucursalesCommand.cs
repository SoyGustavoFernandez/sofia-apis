using SOFIA.Application.Common.Interfaces;

namespace SOFIA.Application.Sucursales.Commands.CargaMasivaSucursales;

public record SucursalImportRow(
    string Nombre,
    string DireccionFisica,
    string NumeroLicencia);

public record CargaMasivaSucursalesCommand(List<SucursalImportRow> Rows) : ICommand<int>;
