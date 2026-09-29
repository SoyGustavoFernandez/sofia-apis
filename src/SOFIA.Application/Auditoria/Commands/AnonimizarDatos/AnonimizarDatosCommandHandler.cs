using MediatR;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Auditoria.Commands.AnonimizarDatos;

public class AnonimizarDatosCommandHandler : IRequestHandler<AnonimizarDatosCommand, Result<Guid>>
{
    public static readonly Error NoImplementadaError =
        Error.Failure("Auditoria.Anonimizacion.NoImplementada", "Data anonymization is not implemented yet; no data was changed.");

    // Real anonymization is postponed; 501 avoids falsely confirming a data-subject request (Ley 29733)
    public Task<Result<Guid>> Handle(AnonimizarDatosCommand request, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Failure<Guid>(NoImplementadaError, 501));
}
