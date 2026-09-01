using SOFIA.Application.Common.Interfaces;

namespace SOFIA.Application.IngredientesActivos.Commands.CargaMasivaIngredientesActivos;

public record IngredienteActivoImportRow(string DenominacionDci, string CodigoAtc);
public record CargaMasivaIngredientesActivosCommand(List<IngredienteActivoImportRow> Rows) : ICommand<int>;
