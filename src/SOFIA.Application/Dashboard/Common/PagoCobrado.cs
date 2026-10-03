using SOFIA.Domain.Enums;

namespace SOFIA.Application.Dashboard.Common;

public record PagoCobrado(Guid VentaId, MetodoPago Metodo, decimal Monto);
