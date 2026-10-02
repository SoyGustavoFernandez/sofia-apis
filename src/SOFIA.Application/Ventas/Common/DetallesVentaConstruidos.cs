using SOFIA.Domain.Entities;

namespace SOFIA.Application.Ventas.Common;

// Sale lines ready to persist, plus whether a simple-prescription line without a registered prescription needs the pharmacist's confirmation
public record DetallesVentaConstruidos(List<DetalleVenta> Detalles, bool RequiereVerificacionReceta);
