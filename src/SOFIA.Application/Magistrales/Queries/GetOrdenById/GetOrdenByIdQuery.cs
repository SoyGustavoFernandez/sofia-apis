using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;
using SOFIA.Application.Magistrales.Queries.GetOrdenes;

namespace SOFIA.Application.Magistrales.Queries.GetOrdenById;

public record GetOrdenByIdQuery(Guid Id) : IRequest<Result<OrdenResumenDto>>;
