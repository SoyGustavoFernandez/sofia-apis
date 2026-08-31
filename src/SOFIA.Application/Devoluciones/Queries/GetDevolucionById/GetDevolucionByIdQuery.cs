using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;
using SOFIA.Application.Devoluciones.Queries.GetDevoluciones;

namespace SOFIA.Application.Devoluciones.Queries.GetDevolucionById;

public record GetDevolucionByIdQuery(Guid Id) : IRequest<Result<DevolucionResumenDto>>;
