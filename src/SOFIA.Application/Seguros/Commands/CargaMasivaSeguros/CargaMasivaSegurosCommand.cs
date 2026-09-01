using SOFIA.Application.Common.Interfaces;

namespace SOFIA.Application.Seguros.Commands.CargaMasivaSeguros;

public record SeguroImportRow(string NombreComercial, string CodigoIdentificadorNacional);
public record CargaMasivaSegurosCommand(List<SeguroImportRow> Rows) : ICommand<int>;
