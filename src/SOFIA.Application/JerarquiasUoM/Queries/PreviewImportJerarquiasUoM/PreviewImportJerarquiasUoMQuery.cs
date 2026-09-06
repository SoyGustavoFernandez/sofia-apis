using MediatR;
using SOFIA.Application.Common.Excel;

namespace SOFIA.Application.JerarquiasUoM.Queries.PreviewImportJerarquiasUoM;

public record PreviewImportJerarquiasUoMQuery(List<ExcelRow> Rows) : IRequest<PreviewResult>;
