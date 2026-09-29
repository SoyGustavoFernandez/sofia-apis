using MediatR;
using SOFIA.Application.Common.Extensions;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Common.Behaviors;

// Registered after TransactionBehavior so the audit row commits or rolls back together with the command
public class AuditBehavior<TRequest, TResponse>(IApplicationDbContext context, ICurrentUser currentUser)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next();

        if (request is not IAuditableCommand auditable || response is not Result { IsSuccess: true } result)
        {
            return response;
        }

        // Only anonymous flows lack an employee; those audit themselves in the handler
        var actor = currentUser.GetEmpleadoId();
        if (actor.IsFailure)
        {
            return response;
        }

        var entry = auditable.GetAuditEntry(GetValue(result));
        if (entry is null)
        {
            return response;
        }

        var evento = AuditoriaEventoSeguridad.Create(actor.Value, entry.Tabla, entry.RegistroId, entry.Evento, null, entry.Detalle, currentUser.ClientIpAddress);

        // A malformed audit entry is a programming error: throwing rolls the whole command back
        if (!evento.IsSuccess)
        {
            throw new InvalidOperationException($"Invalid audit entry for {typeof(TRequest).Name}: {evento.Error.Code}");
        }

        _ = context.AuditoriasEventosSeguridad.Add(evento.Value);
        _ = await context.SaveChangesAsync(cancellationToken);

        return response;
    }

    private static object? GetValue(Result result) =>
        result.GetType().IsGenericType ? result.GetType().GetProperty(nameof(Result<>.Value))!.GetValue(result) : null;
}
