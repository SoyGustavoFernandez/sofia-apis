using SOFIA.Domain.Common;

namespace SOFIA.Domain.Entities;

public sealed class MagistralOrdenProduccion : BaseEntity
{
    private readonly List<MagistralConsumoInsumo> _consumos = [];

    private MagistralOrdenProduccion() { }

    public Guid SucursalId { get; private set; }
    public Guid? RecetaId { get; private set; }
    public Guid ProductoResultanteId { get; private set; }
    public Guid? LoteGeneradoId { get; private set; }
    public decimal? CantidadProducida { get; private set; }
    public Guid QuimicoPreparadorId { get; private set; }
    public DateTime FechaPreparacion { get; private set; }
    public string EstadoProduccion { get; private set; } = string.Empty;

    // Navigation Properties
    public Sucursal? Sucursal { get; private set; }
    public Medicamento? ProductoResultante { get; private set; }
    public LoteInventario? LoteGenerado { get; private set; }
    public Empleado? QuimicoPreparador { get; private set; }
    public IReadOnlyCollection<MagistralConsumoInsumo> Consumos => _consumos.AsReadOnly();

    public static Result<MagistralOrdenProduccion> Create(
        Guid sucursalId,
        Guid? recetaId,
        Guid productoResultanteId,
        Guid? loteGeneradoId,
        decimal? cantidadProducida,
        Guid quimicoPreparadorId,
        string estadoProduccion,
        DateTime? fechaPreparacion = null)
    {
        if (sucursalId == Guid.Empty)
        {
            return Result.Failure<MagistralOrdenProduccion>(Error.Validation("MagistralOrdenProduccion.SucursalId", "Sucursal ID is required."));
        }

        if (productoResultanteId == Guid.Empty)
        {
            return Result.Failure<MagistralOrdenProduccion>(Error.Validation("MagistralOrdenProduccion.ProductoResultanteId", "Producto Resultante ID is required."));
        }

        if (quimicoPreparadorId == Guid.Empty)
        {
            return Result.Failure<MagistralOrdenProduccion>(Error.Validation("MagistralOrdenProduccion.QuimicoPreparadorId", "Químico Preparador ID is required."));
        }

        if (cantidadProducida.HasValue && cantidadProducida.Value <= 0)
        {
            return Result.Failure<MagistralOrdenProduccion>(Error.Validation("MagistralOrdenProduccion.CantidadProducida", "Cantidad Producida must be greater than zero."));
        }

        if (string.IsNullOrWhiteSpace(estadoProduccion))
        {
            return Result.Failure<MagistralOrdenProduccion>(Error.Validation("MagistralOrdenProduccion.EstadoProduccion", "Estado Producción is required."));
        }

        if (estadoProduccion.Length > 20)
        {
            return Result.Failure<MagistralOrdenProduccion>(Error.Validation("MagistralOrdenProduccion.EstadoProduccion", "Estado Producción must not exceed 20 characters."));
        }

        return Result.Success(new MagistralOrdenProduccion
        {
            SucursalId = sucursalId,
            RecetaId = recetaId,
            ProductoResultanteId = productoResultanteId,
            LoteGeneradoId = loteGeneradoId,
            CantidadProducida = cantidadProducida,
            QuimicoPreparadorId = quimicoPreparadorId,
            FechaPreparacion = fechaPreparacion ?? DateTime.UtcNow,
            EstadoProduccion = estadoProduccion
        });
    }

    public Result Update(
        Guid sucursalId,
        Guid? recetaId,
        Guid productoResultanteId,
        Guid? loteGeneradoId,
        decimal? cantidadProducida,
        Guid quimicoPreparadorId,
        string estadoProduccion)
    {
        if (sucursalId == Guid.Empty)
        {
            return Result.Failure(Error.Validation("MagistralOrdenProduccion.SucursalId", "Sucursal ID is required."));
        }

        if (productoResultanteId == Guid.Empty)
        {
            return Result.Failure(Error.Validation("MagistralOrdenProduccion.ProductoResultanteId", "Producto Resultante ID is required."));
        }

        if (quimicoPreparadorId == Guid.Empty)
        {
            return Result.Failure(Error.Validation("MagistralOrdenProduccion.QuimicoPreparadorId", "Químico Preparador ID is required."));
        }

        if (cantidadProducida.HasValue && cantidadProducida.Value <= 0)
        {
            return Result.Failure(Error.Validation("MagistralOrdenProduccion.CantidadProducida", "Cantidad Producida must be greater than zero."));
        }

        if (string.IsNullOrWhiteSpace(estadoProduccion))
        {
            return Result.Failure(Error.Validation("MagistralOrdenProduccion.EstadoProduccion", "Estado Producción is required."));
        }

        if (estadoProduccion.Length > 20)
        {
            return Result.Failure(Error.Validation("MagistralOrdenProduccion.EstadoProduccion", "Estado Producción must not exceed 20 characters."));
        }

        SucursalId = sucursalId;
        RecetaId = recetaId;
        ProductoResultanteId = productoResultanteId;
        LoteGeneradoId = loteGeneradoId;
        CantidadProducida = cantidadProducida;
        QuimicoPreparadorId = quimicoPreparadorId;
        EstadoProduccion = estadoProduccion;

        return Result.Success();
    }
}
