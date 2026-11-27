using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Extensions;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Citas.Commands.ProgramarCita;

public class ProgramarCitaCommandHandler(IApplicationDbContext context, ICurrentUser currentUser) : IRequestHandler<ProgramarCitaCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(ProgramarCitaCommand request, CancellationToken cancellationToken)
    {
        var sucursal = currentUser.GetSucursalId();
        if (sucursal.IsFailure)
        {
            return Result.Failure<Guid>(sucursal.Error);
        }

        // Tenant-filtered lookups: an id from another company must not be stored as a reference
        if (!await context.Pacientes.AnyAsync(p => p.Id == request.PacienteId, cancellationToken))
        {
            return Result.Failure<Guid>(Error.NotFound("Paciente.NotFound", "The specified patient does not exist."), 404);
        }

        if (!await context.ServiciosCatalogo.AnyAsync(s => s.Id == request.ServicioId, cancellationToken))
        {
            return Result.Failure<Guid>(Error.NotFound("ServicioCatalogo.NotFound", "The service does not exist."), 404);
        }

        var cita = CitaServicio.Create(request.PacienteId, request.ServicioId, sucursal.Value, request.FechaHora, request.AceptaRecordatorio, DateTimeOffset.UtcNow);
        if (cita.IsFailure)
        {
            return Result.Failure<Guid>(cita.Error);
        }

        _ = context.CitasServicio.Add(cita.Value!);
        _ = await context.SaveChangesAsync(cancellationToken);
        return Result.Success(cita.Value!.Id, 201);
    }
}
