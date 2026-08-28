using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Empresas.Commands.DeleteEmpresa;

public record DeleteEmpresaCommand(Guid Id) : ICommand;

public class DeleteEmpresaCommandHandler(IApplicationDbContext context)
    : IRequestHandler<DeleteEmpresaCommand, Result>
{
    public async Task<Result> Handle(DeleteEmpresaCommand request, CancellationToken cancellationToken)
    {
        var empresa = await context.Empresas
            .FirstOrDefaultAsync(e => e.Id == request.Id && !e.IsDeleted, cancellationToken);

        if (empresa is null)
        {
            return Result.Failure(Error.NotFound("Empresa.NotFound", "La empresa no existe."), 404);
        }

        empresa.Cancelar();
        empresa.IsDeleted = true;
        empresa.DeletedAt = DateTimeOffset.UtcNow;
        _ = await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
