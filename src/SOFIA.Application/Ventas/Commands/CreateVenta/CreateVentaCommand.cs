using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;
using System.Text.Json;

namespace SOFIA.Application.Ventas.Commands.CreateVenta;

public record CreateVentaCommand(
    Guid? ClienteId,
    Guid? SesionId,
    List<CreateVentaDetailDto> Detalles,
    EstadoVenta Estado = EstadoVenta.Completada,
    Guid? AseguradoraId = null,
    decimal? MontoCubiertoSeguro = null) : ICommand<VentaCreadaDto>;

public record VentaCreadaDto(Guid VentaId, ComprobanteEmitidoDto? Comprobante);

// NOTE: SUNAT integration is simulated (thesis). Hash, URL and CDR fields are placeholders.
// Real integration requires a certified OSE/PSE and a valid digital signature certificate.

public record ComprobanteEmitidoDto(string Tipo, string Numero, string EstadoAceptacion, string? UrlVerificacion, string? UrlXml, string? UrlCdr);

public record CreateVentaDetailDto(
    Guid LoteId,
    decimal Cantidad,
    decimal PrecioUnitario,
    decimal CostoHistorico,
    Guid? RecetaId = null);
