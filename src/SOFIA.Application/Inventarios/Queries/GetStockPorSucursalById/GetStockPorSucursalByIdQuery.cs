using MediatR;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Inventarios.Queries.GetStockPorSucursalById;

public record GetStockPorSucursalByIdQuery(Guid Id) : IRequest<Result<StockPorSucursalDto>>;
