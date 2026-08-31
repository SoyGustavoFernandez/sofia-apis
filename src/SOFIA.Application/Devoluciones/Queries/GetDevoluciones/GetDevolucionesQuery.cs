using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Devoluciones.Queries.GetDevoluciones;

public record DevolucionResumenDto(
    Guid Id,
    Guid ComprobanteOrigenId,
    Guid EmpleadoAutorizaId,
    DateTime FechaDevolucion,
    string MotivoSunatCatalogo,
    string SustentoDescriptivo
);

public record GetDevolucionesQuery(Guid? EmpleadoAutorizaId, DateTime? FechaInicio, DateTime? FechaFin, int PageNumber = 1, int PageSize = 10) : IRequest<Result<PaginatedList<DevolucionResumenDto>>>;
