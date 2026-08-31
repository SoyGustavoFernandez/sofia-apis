using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Ventas.Queries.GetVentaById;

public record GetVentaByIdQuery(Guid Id) : IRequest<Result<VentaConDetalleDto>>;

public record VentaConDetalleDto(
    Guid Id,
    string CodigoVenta,
    DateTime FechaHora,
    decimal Total,
    string Estado,
    string? MotivoAnulacion,
    string EmpleadoNombre,
    string? ClienteNombre,
    List<VentaDetalleDto> Detalles,
    ComprobanteDto? Comprobante);

public record ComprobanteDto(
    string Tipo,
    string Numero,
    string EstadoAceptacion,
    string? UrlVerificacion,
    string? UrlXml,
    string? UrlCdr);

public record VentaDetalleDto(
    Guid Id,
    Guid LoteId,
    string ProductoNombre,
    string NumeroLote,
    decimal Cantidad,
    decimal PrecioUnitario,
    decimal Subtotal);
