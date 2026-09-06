using MediatR;
using SOFIA.Application.Common.Excel;

namespace SOFIA.Application.Medicamentos.Queries.PreviewImportMedicamentos;

public record PreviewImportMedicamentosQuery(List<ExcelRow> Rows) : IRequest<PreviewResult>;
