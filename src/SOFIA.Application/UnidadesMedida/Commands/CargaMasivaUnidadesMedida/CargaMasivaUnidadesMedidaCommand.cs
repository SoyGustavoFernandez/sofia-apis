using SOFIA.Application.Common.Interfaces;

namespace SOFIA.Application.UnidadesMedida.Commands.CargaMasivaUnidadesMedida;

public record UnidadMedidaImportRow(string Codigo, string Descripcion);
public record CargaMasivaUnidadesMedidaCommand(List<UnidadMedidaImportRow> Rows) : ICommand<int>;
