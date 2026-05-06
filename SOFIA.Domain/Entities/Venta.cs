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

    public IReadOnlyCollection<DetalleVenta> Detalles => _detalles.AsReadOnly();

    // Navigation Property
    public Sucursal? Sucursal { get; private set; }

    public static Result<Venta> Create(
        Guid sucursalId,
        Guid empleadoId,
        Guid? clienteId,
        Guid? sesionId,
        List<DetalleVenta> detalles,
        EstadoVenta estado = EstadoVenta.Completada) =>
        sucursalId == Guid.Empty
            ? Result.Failure<Venta>(Error.Validation("Venta.SucursalId", "Sucursal ID is required."))
            : empleadoId == Guid.Empty
                ? Result.Failure<Venta>(Error.Validation("Venta.EmpleadoId", "Empleado ID is required."))
                : detalles == null || detalles.Count == 0
                    ? Result.Failure<Venta>(Error.Validation("Venta.Detalles", "A sale must have at least one detail."))
                    : SuccessVenta(sucursalId, empleadoId, clienteId, sesionId, detalles, estado);

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
            return Result.Failure(Error.Validation("Venta.Anular", "La venta ya está anulada."));
        }

        if (Estado == EstadoVenta.Devuelta)
        {
            return Result.Failure(Error.Validation("Venta.Anular", "No se puede anular una venta que tiene devoluciones. Use el módulo de devoluciones."));
        }

        if (string.IsNullOrWhiteSpace(motivo))
        {
            return Result.Failure(Error.Validation("Venta.MotivoAnulacion", "Debe proporcionar un motivo para la anulación."));
        }

        Estado = EstadoVenta.Anulada;
        MotivoAnulacion = motivo;

        return Result.Success();
    }

    private void CalcularTotales() => MontoTotalBruto = _detalles.Sum(d => d.PrecioFijadoUnidad * d.CantidadVendida);
}
