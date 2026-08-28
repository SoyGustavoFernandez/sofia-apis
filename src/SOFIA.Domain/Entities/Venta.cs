using SOFIA.Domain.Common;
using SOFIA.Domain.Enums;

namespace SOFIA.Domain.Entities;

public sealed class Venta : BaseEntity
{
    private readonly List<DetalleVenta> _detalles = [];

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
}
