using MediatR;
using SOFIA.Application.Common.Excel;

namespace SOFIA.Application.Proveedores.Queries.PreviewImportProveedores;

public record PreviewImportProveedoresQuery(List<ExcelRow> Rows) : IRequest<PreviewResult>;
