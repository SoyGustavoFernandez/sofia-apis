using MediatR;
using SOFIA.Application.Common.Behaviors;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Common.Interfaces;

/// <summary>Marker for void commands — automatically wrapped in a transaction by TransactionBehavior.</summary>
public interface ICommand : IRequest<Result>, IBaseCommand;

/// <summary>Marker for commands that return a value — automatically wrapped in a transaction by TransactionBehavior.</summary>
public interface ICommand<TResponse> : IRequest<Result<TResponse>>, IBaseCommand;
