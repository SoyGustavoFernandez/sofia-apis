using MediatR;
using SOFIA.Application.Common.Excel;

namespace SOFIA.Application.Laboratorios.Queries.PreviewImportLaboratorios;

public record PreviewImportLaboratoriosQuery(List<ExcelRow> Rows) : IRequest<PreviewResult>;
