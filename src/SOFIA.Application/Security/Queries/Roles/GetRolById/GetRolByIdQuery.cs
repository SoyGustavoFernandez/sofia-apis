using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.DTOs;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Security.Queries.Roles.GetRolById;

public record GetRolByIdQuery(Guid Id) : IRequest<Result<RolResponse>>;

public class GetRolByIdQueryHandler(IApplicationDbContext context) : IRequestHandler<GetRolByIdQuery, Result<RolResponse>>
{
    public async Task<Result<RolResponse>> Handle(GetRolByIdQuery request, CancellationToken cancellationToken)
    {
        var rol = await context.Roles
            .Where(r => r.Id == request.Id && !r.IsDeleted)
            .Select(r => new RolResponse(
                r.Id,
                r.NombreRol,
                r.Descripcion,
                r.NivelJerarquia,
                r.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        return rol is null
            ? Result.Failure<RolResponse>(Error.NotFound("Rol.NotFound", "El rol especificado no existe."))
            : Result.Success(rol);
    }
}
