using SOFIA.Application.Common.Interfaces;

namespace SOFIA.Application.JerarquiasUoM.Commands.CargaMasivaJerarquiasUoM;

public record JerarquiaUoMImportRow(
    string Producto,
    string UnidadMayor,
    string UnidadMenor,
    string Multiplicador);

public record CargaMasivaJerarquiasUoMCommand(List<JerarquiaUoMImportRow> Rows) : ICommand<int>;
