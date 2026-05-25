using MediatR;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Security.Queries.GetProfile;

public record ProfileResponse(
    Guid Id,
    string NombreUsuario,
    string EmpleadoNombreCompleto,
    List<string> Roles,
    List<PermissionDto> Permisos);

public record PermissionDto(string Modulo, string Accion);

public record GetProfileQuery(Guid CuentaId) : IRequest<Result<ProfileResponse>>;
