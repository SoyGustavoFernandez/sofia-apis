using MediatR;
using SOFIA.Application.Common.Excel;

namespace SOFIA.Application.Empresas.Queries.PreviewImportEmpresas;

public record PreviewImportEmpresasQuery(List<ExcelRow> Rows) : IRequest<PreviewResult>;
