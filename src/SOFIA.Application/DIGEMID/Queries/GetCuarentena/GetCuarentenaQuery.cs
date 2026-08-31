using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.DIGEMID.Queries.GetCuarentena;

public record CuarentenaResumenDto(Guid Id, Guid SucursalId, Guid LoteId, decimal CantidadAislada, string MotivoAislamiento, string EstadoResolucion, DateTime FechaIngresoCuarentena);

public record GetCuarentenaQuery(Guid? SucursalId, string? EstadoResolucion, DateTime? FechaInicio, DateTime? FechaFin, int PageNumber = 1, int PageSize = 10) : IRequest<Result<PaginatedList<CuarentenaResumenDto>>>;
