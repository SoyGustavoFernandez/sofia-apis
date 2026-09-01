using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Pacientes.Commands.CargaMasivaPacientes;

public class CargaMasivaPacientesCommandHandler(IApplicationDbContext context)
    : IRequestHandler<CargaMasivaPacientesCommand, Result<int>>
{
    public async Task<Result<int>> Handle(CargaMasivaPacientesCommand request, CancellationToken cancellationToken)
    {
        var saved = 0;
        foreach (var row in request.Rows)
        {
            if (!DateOnly.TryParseExact(row.FechaNacimiento, "dd/MM/yyyy", out var fecha))
            {
                continue;
            }

            var result = PacienteCliente.Create(row.DocIdentidadGub, row.NombreApellidos, fecha, row.ContactoPrimario);
            if (result.IsSuccess)
            {
                _ = context.Pacientes.Add(result.Value);
                saved++;
            }
        }
        _ = await context.SaveChangesAsync(cancellationToken);
        return Result.Success<int>(saved);
    }
}
