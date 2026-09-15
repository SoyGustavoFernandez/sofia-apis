using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Ventas.Common;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;
using System.Text.Json;

namespace SOFIA.Application.Ventas.Commands.CreateVenta;

public record CreateVentaCommand(
    Guid? ClienteId,
    Guid? SesionId,
    List<CreateVentaDetailDto> Detalles,
    List<CreateVentaPagoDto> Pagos,
    EstadoVenta Estado = EstadoVenta.Completada,
    Guid? AseguradoraId = null,
    decimal? MontoCubiertoSeguro = null) : ICommand<VentaCreadaDto>;

public record CreateVentaPagoDto(MetodoPago MetodoPago, decimal MontoPagado, string? ReferenciaOperacion);

public record VentaCreadaDto(Guid VentaId, ComprobanteEmitidoDto? Comprobante);

public record CreateVentaDetailDto(
    Guid LoteId,
    decimal Cantidad,
    decimal PrecioUnitario,
    decimal CostoHistorico,
    Guid? RecetaId = null);
