using SOFIA.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SOFIA.Application.Pacientes.Commands.DeletePaciente;

public class DeletePacienteCommandHandler(IApplicationDbContext context) : IRequestHandler<DeletePacienteCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(DeletePacienteCommand request, CancellationToken cancellationToken)
    {
        var entity = await context.Pacientes
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (entity == null)
        {
            return Result.Failure<Guid>(Error.NotFound("NotFound", "Record not found."), 404);
        }

        if (await IsReferencedAsync(request.Id, cancellationToken))
        {
            return Result.Failure<Guid>(
                Error.Conflict("Paciente.InUse", "Cannot delete a patient referenced by prescriptions, sales, appointments or immunizations."),
                409);
        }

        _ = context.Pacientes.Remove(entity);

        _ = await context.SaveChangesAsync(cancellationToken);
        return Result.Success(entity.Id);
    }

    private async Task<bool> IsReferencedAsync(Guid id, CancellationToken cancellationToken) =>
        await context.Recetas.AnyAsync(r => r.ClienteId == id && !r.IsDeleted, cancellationToken)
        || await context.Ventas.AnyAsync(v => v.ClienteId == id && !v.IsDeleted, cancellationToken)
        || await context.ServiciosAgenda.AnyAsync(s => s.ClienteId == id && !s.IsDeleted, cancellationToken)
        || await context.ServiciosClinicosInmunizacion.AnyAsync(s => s.ClienteId == id && !s.IsDeleted, cancellationToken);
}
