using MediatR;
using SOFIA.Application.Common.Excel;

namespace SOFIA.Application.Profesionales.Queries.PreviewImportProfesionalesSalud;

public record PreviewImportProfesionalesSaludQuery(List<ExcelRow> Rows) : IRequest<PreviewResult>;
