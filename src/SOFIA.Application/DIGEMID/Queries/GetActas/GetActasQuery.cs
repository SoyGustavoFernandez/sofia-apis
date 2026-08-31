using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.DIGEMID.Queries.GetActas;

public record ActaResumenDto(Guid Id, string NumeroResolucionInterna, DateTime FechaEjecucion, string EmpresaResiduosBiocontaminados);

public record GetActasQuery(DateTime? FechaInicio, DateTime? FechaFin, int PageNumber = 1, int PageSize = 10) : IRequest<Result<PaginatedList<ActaResumenDto>>>;
