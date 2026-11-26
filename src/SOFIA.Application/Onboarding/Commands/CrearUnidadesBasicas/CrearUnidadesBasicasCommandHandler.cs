using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Onboarding.Commands.CrearUnidadesBasicas;

public class CrearUnidadesBasicasCommandHandler(IApplicationDbContext context, ICurrentUser currentUser)
    : IRequestHandler<CrearUnidadesBasicasCommand, Result<int>>
{
    public async Task<Result<int>> Handle(CrearUnidadesBasicasCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsInRole(Rol.AdminRoleName))
        {
            return Result.Failure<int>(OnboardingErrors.SoloAdministrador, 403);
        }

        if (await context.UnidadesMedida.AnyAsync(u => !u.IsDeleted, cancellationToken))
        {
            return Result.Success<int>(0);
        }

        foreach (var (codigo, descripcion) in UnidadMedida.Basicas)
        {
            _ = context.UnidadesMedida.Add(UnidadMedida.Create(codigo, descripcion).Value!);
        }

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success<int>(UnidadMedida.Basicas.Count);
    }
}
