using MediatR;
using SOFIA.Application.Common.Excel;

namespace SOFIA.Application.DIGEMID.Queries.PreviewImportDigemid;

public record PreviewImportDigemidQuery(List<ExcelRow> Rows) : IRequest<PreviewResult>;
