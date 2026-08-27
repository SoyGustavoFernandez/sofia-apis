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

public class GetVentaByIdQueryHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser) : IRequestHandler<GetVentaByIdQuery, Result<VentaConDetalleDto>>
{
    public async Task<Result<VentaConDetalleDto>> Handle(GetVentaByIdQuery request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || string.IsNullOrEmpty(currentUser.SucursalId))
        {
            return Result.Failure<VentaConDetalleDto>(Error.Unauthorized("Venta.Auth", "Usuario no autenticado."));
        }

        var venta = await context.Ventas
            .AsNoTracking()
            .Include(v => v.Empleado)
            .Include(v => v.Detalles)
                .ThenInclude(d => d.Lote)
                    .ThenInclude(l => l!.Producto)
            .FirstOrDefaultAsync(v => v.Id == request.Id, cancellationToken);

        var comprobante = await context.SUNATComprobantesEmitidos
            .Include(c => c.Serie)
            .FirstOrDefaultAsync(c => c.TransaccionId == request.Id && !c.IsDeleted, cancellationToken);

        if (venta == null)
        {
            return Result.Failure<VentaConDetalleDto>(Error.NotFound("Venta.NotFound", $"No se encontrÃ³ la venta con ID {request.Id}"));
        }

        // Verify the sale belongs to the user's branch
        if (venta.SucursalId.ToString().ToLower() != currentUser.SucursalId.ToLower())
        {
            return Result.Failure<VentaConDetalleDto>(Error.Forbidden("Venta.Forbidden", "No tiene permiso para ver esta venta."));
        }

        var dto = new VentaConDetalleDto(
            venta.Id,
            venta.Id.ToString()[..8].ToUpper(),
            venta.FechaHoraUtc,
            venta.MontoTotalBruto,
            venta.Estado.ToString(),
            venta.MotivoAnulacion,
            venta.Empleado != null ? $"{venta.Empleado.Nombres} {venta.Empleado.Apellido_Paterno}" : "N/A",
            venta.ClienteId?.ToString()[..8] ?? "PÃºblico General",
            [.. venta.Detalles.Select(d => new VentaDetalleDto(
                d.Id,
                d.LoteId,
                d.Lote?.Producto?.NombreComercial ?? "Producto desconocido",
                d.Lote?.NumeroLoteMfr ?? "N/A",
                d.CantidadVendida,
                d.PrecioFijadoUnidad,
                d.CantidadVendida * d.PrecioFijadoUnidad
            ))],
            comprobante != null ? new ComprobanteDto(
                comprobante.Serie?.TipoComprobante.ToString() ?? "Boleta",
                $"{comprobante.Serie?.PrefijoSerie}-{comprobante.NumeroCorrelativo:D8}",
                comprobante.EstadoAceptacion,
                comprobante.UrlPublicaVerificacion,
                comprobante.RutaArchivoXml,
                comprobante.RutaArchivoCdr
            ) : null
        );

        return Result.Success(dto);
    }
}
