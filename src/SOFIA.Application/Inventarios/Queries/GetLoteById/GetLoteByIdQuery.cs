using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Inventarios.Queries.GetLoteById;

public record GetLoteByIdQuery(Guid Id) : IRequest<Result<LoteInventarioDto>>;
