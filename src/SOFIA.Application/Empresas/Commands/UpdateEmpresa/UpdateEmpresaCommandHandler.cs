using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.ValueObjects;

namespace SOFIA.Application.Empresas.Commands.UpdateEmpresa;

public class UpdateEmpresaCommandHandler(IApplicationDbContext context)
    : IRequestHandler<UpdateEmpresaCommand, Result>
{
    public async Task<Result> Handle(UpdateEmpresaCommand request, CancellationToken cancellationToken)
    {
        var empresa = await context.Empresas
            .FirstOrDefaultAsync(e => e.Id == request.Id && !e.IsDeleted, cancellationToken);

        if (empresa is null)
        {
            return Result.Failure(Error.NotFound("Empresa.NotFound", "La empresa no existe."), 404);
        }

        if (request.RUC is not null)
        {
            var rucVo = Ruc.Create(request.RUC).Value!;
            var rucTomado = await context.Empresas
                .AnyAsync(e => e.RUC == rucVo && e.Id != request.Id && !e.IsDeleted, cancellationToken);
            if (rucTomado)
            {
                return Result.Failure(Error.Conflict("Empresa.RUC.Duplicado", "Ya existe otra empresa con este RUC."), 409);
            }
        }

        var result = empresa.Update(request.Nombre, request.RUC);
        if (result.IsFailure)
        {
            return result;
        }

        _ = await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
