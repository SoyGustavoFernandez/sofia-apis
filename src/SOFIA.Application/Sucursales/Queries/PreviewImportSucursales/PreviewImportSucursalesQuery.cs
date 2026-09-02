using MediatR;
using SOFIA.Application.Common.Excel;

namespace SOFIA.Application.Sucursales.Queries.PreviewImportSucursales;

public record PreviewImportSucursalesQuery(List<ExcelRow> Rows) : IRequest<PreviewResult>;
