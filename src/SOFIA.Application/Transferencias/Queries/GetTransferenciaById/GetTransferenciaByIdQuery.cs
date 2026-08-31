using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Transferencias.Queries.GetTransferenciaById;

public record GetTransferenciaByIdQuery(Guid Id) : IRequest<Result<TransferenciaDto>>;
