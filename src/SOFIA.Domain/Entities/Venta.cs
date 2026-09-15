using SOFIA.Domain.Common;
using SOFIA.Domain.Enums;

namespace SOFIA.Domain.Entities;

public sealed class Venta : BaseEntity
{
    private readonly List<DetalleVenta> _detalles = [];
    private readonly List<VentaPago> _pagos = [];

    private Venta() { }

    public Guid SucursalId { get; private set; }
    public Guid EmpleadoId { get; private set; }
    public Guid? ClienteId { get; private set; }
    public Guid? SesionId { get; private set; }
    public DateTime FechaHoraUtc { get; private set; }
    public decimal MontoTotalBruto { get; private set; }
    public EstadoVenta Estado { get; private set; }
    public string? MotivoAnulacion { get; private set; }

    // Navigation properties
    public Sucursal? Sucursal { get; }
    public Empleado? Empleado { get; }
    public PacienteCliente? Cliente { get; }
    public PosSesionCaja? Sesion { get; }

    public IReadOnlyCollection<DetalleVenta> Detalles => _detalles.AsReadOnly();
    public IReadOnlyCollection<VentaPago> Pagos => _pagos.AsReadOnly();

    public static Result<Venta> Create(
        Guid sucursalId,
        Guid empleadoId,
        Guid? clienteId,
        Guid? sesionId,
        List<DetalleVenta> detalles,
        EstadoVenta estado = EstadoVenta.Completada)
    {
        if (sucursalId == Guid.Empty)
        {
            return Result.Failure<Venta>(Error.Validation("Venta.SucursalId", "Sucursal ID is required."));
        }

        if (empleadoId == Guid.Empty)
        {
            return Result.Failure<Venta>(Error.Validation("Venta.EmpleadoId", "Empleado ID is required."));
        }

        if (detalles == null || detalles.Count == 0)
        {
            return Result.Failure<Venta>(Error.Validation("Venta.Detalles", "A sale must have at least one detail."));
        }

        return SuccessVenta(sucursalId, empleadoId, clienteId, sesionId, detalles, estado);
    }

    private static Result<Venta> SuccessVenta(Guid sucursalId, Guid empleadoId, Guid? clienteId, Guid? sesionId, List<DetalleVenta> detalles, EstadoVenta estado)
    {
        var venta = new Venta
        {
            SucursalId = sucursalId,
            EmpleadoId = empleadoId,
            ClienteId = clienteId,
            SesionId = sesionId,
            FechaHoraUtc = DateTime.UtcNow,
            Estado = estado
        };

        foreach (var detalle in detalles)
        {
            detalle.SetVentaId(venta.Id);
            venta._detalles.Add(detalle);
        }

        venta.CalcularTotales();

        return Result.Success(venta);
    }

    public Result Anular(string motivo)
    {
        if (Estado == EstadoVenta.Anulada)
        {
            return Result.Failure(Error.Validation("Venta.Anular", "The sale is already cancelled."));
        }

        if (Estado == EstadoVenta.Devuelta)
        {
            return Result.Failure(Error.Validation("Venta.Anular", "Cannot cancel a sale with returns. Use the returns module."));
        }

        if (string.IsNullOrWhiteSpace(motivo))
        {
            return Result.Failure(Error.Validation("Venta.MotivoAnulacion", "A reason for cancellation must be provided."));
        }

        Estado = EstadoVenta.Anulada;
        MotivoAnulacion = motivo;

        return Result.Success();
    }

    private void CalcularTotales() => MontoTotalBruto = _detalles.Sum(d => d.PrecioFijadoUnidad * d.CantidadVendida);

    public Result ActualizarDetalles(List<DetalleVenta> nuevosDetalles, Guid? clienteId)
    {
        if (Estado != EstadoVenta.Pendiente)
        {
            return Result.Failure(Error.Validation("Venta.ActualizarDetalles", "Only a pending sale can have its items modified."));
        }

        if (nuevosDetalles == null || nuevosDetalles.Count == 0)
        {
            return Result.Failure(Error.Validation("Venta.Detalles", "A sale must have at least one detail."));
        }

        _detalles.Clear();
        foreach (var detalle in nuevosDetalles)
        {
            detalle.SetVentaId(Id);
            _detalles.Add(detalle);
        }

        ClienteId = clienteId;
        CalcularTotales();

        return Result.Success();
    }

    public Result RegistrarPagos(List<VentaPago> pagos, decimal montoCubiertoSeguro = 0)
    {
        if (Estado == EstadoVenta.Anulada || Estado == EstadoVenta.Devuelta)
        {
            return Result.Failure(Error.Validation("Venta.Pagos", "Cannot register payments on a cancelled or returned sale."));
        }

        if (pagos == null || pagos.Count == 0)
        {
            return Result.Failure(Error.Validation("Venta.Pagos", "At least one payment is required."));
        }

        var montoAPagar = MontoTotalBruto - montoCubiertoSeguro;
        var totalPagado = pagos.Sum(p => p.MontoPagado);

        if (totalPagado < montoAPagar)
        {
            return Result.Failure(Error.Validation("Venta.Pagos", "The sum of payments is insufficient to cover the sale total."));
        }

        var vuelto = totalPagado - montoAPagar;
        if (vuelto > 0 && !pagos.Exists(p => p.MetodoPago == MetodoPago.Efectivo))
        {
            return Result.Failure(Error.Validation("Venta.Pagos", "Change can only be given when a cash payment is included."));
        }

        foreach (var pago in pagos)
        {
            pago.SetVentaId(Id);
            _pagos.Add(pago);
        }

        Estado = EstadoVenta.Completada;

        return Result.Success();
    }
}
