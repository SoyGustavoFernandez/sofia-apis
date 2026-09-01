using MediatR;
using SOFIA.Application.Common.Excel;

namespace SOFIA.Application.Seguros.Queries.PreviewImportSeguros;

public record PreviewImportSegurosQuery(List<ExcelRow> Rows) : IRequest<PreviewResult>;
