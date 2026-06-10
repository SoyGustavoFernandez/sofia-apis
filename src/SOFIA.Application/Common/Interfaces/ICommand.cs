using MediatR;
using SOFIA.Application.Common.Behaviors;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Common.Interfaces;

/// <summary>Marker for void commands — automáticamente envueltos en transacción por TransactionBehavior.</summary>
public interface ICommand : IRequest<Result>, IBaseCommand;

/// <summary>Marker for commands que retornan un valor — automáticamente envueltos en transacción por TransactionBehavior.</summary>
public interface ICommand<TResponse> : IRequest<Result<TResponse>>, IBaseCommand;
