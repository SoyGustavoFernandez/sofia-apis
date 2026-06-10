using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;

namespace SOFIA.Application.Common.Behaviors;

// Marcador interno compartido con ICommand / ICommand<T>
public interface IBaseCommand;

public class TransactionBehavior<TRequest, TResponse>(IApplicationDbContext context)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // Aplica transacción a requests que implementan IBaseCommand (ICommand / ICommand<T>)
        // o que siguen la convención de nombre "*Command" para compatibilidad con código existente
        var isCommand = request is IBaseCommand
            || typeof(TRequest).Name.EndsWith("Command", StringComparison.Ordinal);

        if (!isCommand)
        {
            return await next();
        }

        var strategy = context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var response = await next();
                await transaction.CommitAsync(cancellationToken);
                return response;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }
}
