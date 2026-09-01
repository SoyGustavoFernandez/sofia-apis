using MediatR;
using SOFIA.Application.Common.Excel;

namespace SOFIA.Application.IngredientesActivos.Queries.PreviewImportIngredientesActivos;

public record PreviewImportIngredientesActivosQuery(List<ExcelRow> Rows) : IRequest<PreviewResult>;
