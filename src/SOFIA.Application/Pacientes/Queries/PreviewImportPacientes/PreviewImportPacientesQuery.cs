using MediatR;
using SOFIA.Application.Common.Excel;

namespace SOFIA.Application.Pacientes.Queries.PreviewImportPacientes;

public record PreviewImportPacientesQuery(List<ExcelRow> Rows) : IRequest<PreviewResult>;
