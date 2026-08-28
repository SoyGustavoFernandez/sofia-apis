using SOFIA.Domain.Common;

namespace SOFIA.Domain.Entities;

public sealed class LoteInventario : BaseEntity
{
    private LoteInventario() { }

    public Guid ProductoId { get; private set; }
    public string NumeroLoteMfr { get; private set; } = string.Empty;
    public DateTimeOffset? FechaFabricacion { get; private set; }
    public DateTimeOffset FechaCaducidad { get; private set; }

    // Navigation Property
    public Medicamento? Producto { get; }

    public static Result<LoteInventario> Create(
        Guid productoId,
        string numeroLoteMfr,
        DateTimeOffset? fechaFabricacion,
        DateTimeOffset fechaCaducidad)
    {
        if (productoId == Guid.Empty)
        {
            return Result.Failure<LoteInventario>(Error.Validation("LoteInventario.ProductoId", "Producto ID is required."));
        }

        if (string.IsNullOrWhiteSpace(numeroLoteMfr))
        {
            return Result.Failure<LoteInventario>(Error.Validation("LoteInventario.NumeroLoteMfr", "Batch Number (Mfr) is required."));
        }

        if (numeroLoteMfr.Length > 100)
        {
            return Result.Failure<LoteInventario>(Error.Validation("LoteInventario.NumeroLoteMfr", "Batch Number must not exceed 100 characters."));
        }

        if (fechaCaducidad <= DateTimeOffset.UtcNow)
        {
            return Result.Failure<LoteInventario>(Error.Validation("LoteInventario.FechaCaducidad", "Expiration date must be in the future."));
        }

        if (fechaFabricacion.HasValue && fechaFabricacion.Value > DateTimeOffset.UtcNow)
        {
            return Result.Failure<LoteInventario>(Error.Validation("LoteInventario.FechaFabricacion", "Manufacture date cannot be in the future."));
        }

        if (fechaFabricacion.HasValue && fechaFabricacion.Value > fechaCaducidad)
        {
            return Result.Failure<LoteInventario>(Error.Validation("LoteInventario.FechaFabricacion", "Manufacture date cannot be after expiration date."));
        }

        return Result.Success(new LoteInventario
        {
            ProductoId = productoId,
            NumeroLoteMfr = numeroLoteMfr,
            FechaFabricacion = fechaFabricacion,
            FechaCaducidad = fechaCaducidad
        });
    }

    public Result Update(
        string numeroLoteMfr,
        DateTimeOffset? fechaFabricacion,
        DateTimeOffset fechaCaducidad)
    {
        if (string.IsNullOrWhiteSpace(numeroLoteMfr))
        {
            return Result.Failure(Error.Validation("LoteInventario.NumeroLoteMfr", "Batch Number (Mfr) is required."));
        }

        if (numeroLoteMfr.Length > 100)
        {
            return Result.Failure(Error.Validation("LoteInventario.NumeroLoteMfr", "Batch Number must not exceed 100 characters."));
        }

        if (fechaCaducidad <= DateTimeOffset.UtcNow)
        {
            return Result.Failure(Error.Validation("LoteInventario.FechaCaducidad", "Expiration date must be in the future."));
        }

        if (fechaFabricacion.HasValue && fechaFabricacion.Value > DateTimeOffset.UtcNow)
        {
            return Result.Failure(Error.Validation("LoteInventario.FechaFabricacion", "Manufacture date cannot be in the future."));
        }

        NumeroLoteMfr = numeroLoteMfr;
        FechaFabricacion = fechaFabricacion;
        FechaCaducidad = fechaCaducidad;

        return Result.Success();
    }
}
