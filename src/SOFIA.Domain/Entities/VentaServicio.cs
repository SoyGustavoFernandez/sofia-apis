using SOFIA.Domain.Common;

namespace SOFIA.Domain.Entities;

// A service charged in a sale; kept apart from product lines because it has no lot, stock or returns
public sealed class VentaServicio : BaseEntity
{
    private VentaServicio() { }

    public Guid VentaId { get; private set; }
    public Guid ServicioId { get; private set; }
    public Guid CitaId { get; private set; }
    public decimal Precio { get; private set; }

    public static Result<VentaServicio> Create(Guid servicioId, Guid citaId, decimal precio) => servicioId == Guid.Empty || citaId == Guid.Empty || precio <= 0
            ? Result.Failure<VentaServicio>(Error.Validation("VentaServicio.Datos", "Service, appointment and a positive price are required."))
            : Result.Success(new VentaServicio { ServicioId = servicioId, CitaId = citaId, Precio = precio });

    internal void SetVentaId(Guid ventaId) => VentaId = ventaId;
}
