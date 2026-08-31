using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Devoluciones.Commands.ProcesarDevolucion;

public record DevolucionDetalleDto(Guid DetalleVentaId, decimal CantidadDevuelta, Domain.Enums.DestinoDevolucion DestinoFisicoLogico);

public record ProcesarDevolucionCommand(
    Guid ComprobanteOrigenId,
    Guid EmpleadoAutorizaId,
    string MotivoSunatCatalogo,
    string SustentoDescriptivo,
    List<DevolucionDetalleDto> Detalles
) : ICommand<Guid>;
