using MediatR;
using SOFIA.Application.Common.Excel;

namespace SOFIA.Application.Security.Queries.Roles.PreviewImportRoles;

public record PreviewImportRolesQuery(List<ExcelRow> Rows) : IRequest<PreviewResult>;
