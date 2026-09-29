using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Empleados.Commands.CreateEmpleado;

public class CreateEmpleadoCommandHandler(IApplicationDbContext context, ICurrentUser currentUser) : IRequestHandler<CreateEmpleadoCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateEmpleadoCommand request, CancellationToken cancellationToken)
    {
        // Tenant-filtered lookup: a branch of another company is reported as missing
        var sucursalExists = await context.Sucursales.AnyAsync(s => s.Id == request.Sucursal_Base_ID, cancellationToken);
        if (!sucursalExists)
        {
            return Result.Failure<Guid>(Error.NotFound("Sucursal.NotFound", "La sucursal no existe."), 404);
        }

        var tenantId = Guid.TryParse(currentUser.EmpresaId, out var id) ? id : (Guid?)null;

        var result = Empleado.Create(
            request.Sucursal_Base_ID,
            request.Nombres,
            request.Apellido_Paterno,
            request.Apellido_Materno,
            request.Licencia_Prof,
            request.Huella_Biometrica,
            tenantId);

        if (!result.IsSuccess)
        {
            return Result.Failure<Guid>(result.Error);
        }

        if (!string.IsNullOrWhiteSpace(request.Licencia_Prof)
            && await context.Empleados.AnyAsync(e => e.Licencia_Prof == request.Licencia_Prof && !e.IsDeleted, cancellationToken))
        {
            return Result.Failure<Guid>(Error.Conflict("Empleado.LicenciaProf.Duplicado", "Another employee already uses this professional license."), 409);
        }

        _ = context.Empleados.Add(result.Value);

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success(result.Value.Id, 201);
    }
}
