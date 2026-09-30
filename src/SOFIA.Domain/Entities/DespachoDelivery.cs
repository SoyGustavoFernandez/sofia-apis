using SOFIA.Domain.Common;

namespace SOFIA.Domain.Entities;

public sealed class DespachoDelivery : BaseEntity
{

    private DespachoDelivery() { } // Required for EF Core

    public Guid VentaId { get; private set; }
    public string PlataformaServicio { get; private set; } = string.Empty;
    public string? CodigoRastreo { get; private set; }
    public Enums.EstadoDespacho EstadoDespacho { get; private set; }
    public string DireccionEntrega { get; private set; } = string.Empty;
    public string? RepartidorNombre { get; private set; }
    public string? EvidenciaFotograficaUrl { get; private set; }

    // Navigation Property
    public Venta? Venta { get; }

    public static Result<DespachoDelivery> Create(
        Guid ventaId,
        string plataformaServicio,
        string? codigoRastreo,
        Enums.EstadoDespacho estadoDespacho,
        string direccionEntrega,
        string? repartidorNombre,
        string? evidenciaFotograficaUrl)
    {
        if (ventaId == Guid.Empty)
        {
            return Result.Failure<DespachoDelivery>(Error.Validation("DespachoDelivery.VentaId", "Venta ID is required."));
        }

        if (string.IsNullOrWhiteSpace(plataformaServicio))
        {
            return Result.Failure<DespachoDelivery>(Error.Validation("DespachoDelivery.PlataformaServicio", "Plataforma de servicio is required."));
        }

        if (plataformaServicio.Length > 50)
        {
            return Result.Failure<DespachoDelivery>(Error.Validation("DespachoDelivery.PlataformaServicio", "Plataforma de servicio must not exceed 50 characters."));
        }

        if (string.IsNullOrWhiteSpace(direccionEntrega))
        {
            return Result.Failure<DespachoDelivery>(Error.Validation("DespachoDelivery.DireccionEntrega", "Dirección de entrega is required."));
        }

        if (direccionEntrega.Length > 255)
        {
            return Result.Failure<DespachoDelivery>(Error.Validation("DespachoDelivery.DireccionEntrega", "Dirección de entrega must not exceed 255 characters."));
        }

        if (codigoRastreo != null && codigoRastreo.Length > 100)
        {
            return Result.Failure<DespachoDelivery>(Error.Validation("DespachoDelivery.CodigoRastreo", "Código de rastreo must not exceed 100 characters."));
        }

        if (repartidorNombre != null && repartidorNombre.Length > 150)
        {
            return Result.Failure<DespachoDelivery>(Error.Validation("DespachoDelivery.RepartidorNombre", "Nombre de repartidor must not exceed 150 characters."));
        }

        if (evidenciaFotograficaUrl != null && evidenciaFotograficaUrl.Length > 500)
        {
            return Result.Failure<DespachoDelivery>(Error.Validation("DespachoDelivery.EvidenciaFotograficaUrl", "Evidencia fotográfica URL must not exceed 500 characters."));
        }

        return Result.Success(new DespachoDelivery
        {
            VentaId = ventaId,
            PlataformaServicio = plataformaServicio,
            CodigoRastreo = codigoRastreo,
            EstadoDespacho = estadoDespacho,
            DireccionEntrega = direccionEntrega,
            RepartidorNombre = repartidorNombre,
            EvidenciaFotograficaUrl = evidenciaFotograficaUrl
        });
    }

    public Result Update(
        string plataformaServicio,
        string? codigoRastreo,
        Enums.EstadoDespacho estadoDespacho,
        string direccionEntrega,
        string? repartidorNombre,
        string? evidenciaFotograficaUrl)
    {
        if (string.IsNullOrWhiteSpace(plataformaServicio))
        {
            return Result.Failure(Error.Validation("DespachoDelivery.PlataformaServicio", "Plataforma de servicio is required."));
        }

        if (plataformaServicio.Length > 50)
        {
            return Result.Failure(Error.Validation("DespachoDelivery.PlataformaServicio", "Plataforma de servicio must not exceed 50 characters."));
        }

        if (string.IsNullOrWhiteSpace(direccionEntrega))
        {
            return Result.Failure(Error.Validation("DespachoDelivery.DireccionEntrega", "Dirección de entrega is required."));
        }

        if (direccionEntrega.Length > 255)
        {
            return Result.Failure(Error.Validation("DespachoDelivery.DireccionEntrega", "Dirección de entrega must not exceed 255 characters."));
        }

        if (codigoRastreo != null && codigoRastreo.Length > 100)
        {
            return Result.Failure(Error.Validation("DespachoDelivery.CodigoRastreo", "Código de rastreo must not exceed 100 characters."));
        }

        if (repartidorNombre != null && repartidorNombre.Length > 150)
        {
            return Result.Failure(Error.Validation("DespachoDelivery.RepartidorNombre", "Nombre de repartidor must not exceed 150 characters."));
        }

        if (evidenciaFotograficaUrl != null && evidenciaFotograficaUrl.Length > 500)
        {
            return Result.Failure(Error.Validation("DespachoDelivery.EvidenciaFotograficaUrl", "Evidencia fotográfica URL must not exceed 500 characters."));
        }

        if (estadoDespacho != EstadoDespacho && !PuedeTransicionar(EstadoDespacho, estadoDespacho))
        {
            return Result.Failure(Error.Validation("Delivery.Estado.TransicionInvalida", $"A dispatch cannot move from {EstadoDespacho} to {estadoDespacho}."));
        }

        PlataformaServicio = plataformaServicio;
        CodigoRastreo = codigoRastreo;
        EstadoDespacho = estadoDespacho;
        DireccionEntrega = direccionEntrega;
        RepartidorNombre = repartidorNombre;
        EvidenciaFotograficaUrl = evidenciaFotograficaUrl;

        return Result.Success();
    }

    // Preparando -> En_Camino -> Entregado; Devuelto is only reachable before delivery; Entregado and Devuelto are final
    public static bool PuedeTransicionar(Enums.EstadoDespacho actual, Enums.EstadoDespacho nuevo) => (actual, nuevo) switch
    {
        (Enums.EstadoDespacho.Preparando, Enums.EstadoDespacho.En_Camino) => true,
        (Enums.EstadoDespacho.Preparando, Enums.EstadoDespacho.Devuelto) => true,
        (Enums.EstadoDespacho.En_Camino, Enums.EstadoDespacho.Entregado) => true,
        (Enums.EstadoDespacho.En_Camino, Enums.EstadoDespacho.Devuelto) => true,
        _ => false,
    };
}
