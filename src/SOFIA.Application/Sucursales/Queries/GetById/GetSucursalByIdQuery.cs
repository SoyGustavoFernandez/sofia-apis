using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Sucursales.Queries.GetById;

public record GetSucursalByIdQuery(Guid Id) : IRequest<Result<SucursalDto>>;
