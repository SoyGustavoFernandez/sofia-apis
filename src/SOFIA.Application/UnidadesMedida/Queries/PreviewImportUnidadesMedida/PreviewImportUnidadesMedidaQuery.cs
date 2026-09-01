using MediatR;
using SOFIA.Application.Common.Excel;

namespace SOFIA.Application.UnidadesMedida.Queries.PreviewImportUnidadesMedida;

public record PreviewImportUnidadesMedidaQuery(List<ExcelRow> Rows) : IRequest<PreviewResult>;
