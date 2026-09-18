using SOFIA.Domain.Common;

namespace SOFIA.Domain.Entities;

public sealed class PresentacionVenta : BaseEntity
{
    private PresentacionVenta() { } // Required for EF Core

    public Guid ProductoId { get; private set; }
    public Guid UnidadVentaId { get; private set; }
    public string Descripcion { get; private set; } = null!;
    public decimal CantidadUnidadesBase { get; private set; }
    public decimal PrecioVenta { get; private set; }

    // Navigation Properties
    public Medicamento? Producto { get; }
    public UnidadMedida? UnidadVenta { get; }

    public static Result<PresentacionVenta> Create(
        Guid productoId,
        Guid unidadVentaId,
        string descripcion,
        decimal cantidadUnidadesBase,
        decimal precioVenta)
    {
        if (productoId == Guid.Empty)
        {
            return Result.Failure<PresentacionVenta>(Error.Validation("PresentacionVenta.ProductoId", "Producto ID is required."));
        }

        if (unidadVentaId == Guid.Empty)
        {
            return Result.Failure<PresentacionVenta>(Error.Validation("PresentacionVenta.UnidadVentaId", "Unidad de Venta is required."));
        }

        if (string.IsNullOrWhiteSpace(descripcion))
        {
            return Result.Failure<PresentacionVenta>(Error.Validation("PresentacionVenta.Descripcion", "Descripcion is required."));
        }

        if (descripcion.Length > 100)
        {
            return Result.Failure<PresentacionVenta>(Error.Validation("PresentacionVenta.Descripcion", "Descripcion cannot exceed 100 characters."));
        }

        if (cantidadUnidadesBase <= 0)
        {
            return Result.Failure<PresentacionVenta>(Error.Validation("PresentacionVenta.CantidadUnidadesBase", "Cantidad Unidades Base must be greater than zero."));
        }

        if (precioVenta < 0)
        {
            return Result.Failure<PresentacionVenta>(Error.Validation("PresentacionVenta.PrecioVenta", "Precio Venta cannot be negative."));
        }

        return Result.Success(new PresentacionVenta
        {
            ProductoId = productoId,
            UnidadVentaId = unidadVentaId,
            Descripcion = descripcion,
            CantidadUnidadesBase = cantidadUnidadesBase,
            PrecioVenta = precioVenta
        });
    }

    public Result Update(
        Guid unidadVentaId,
        string descripcion,
        decimal cantidadUnidadesBase,
        decimal precioVenta)
    {
        if (unidadVentaId == Guid.Empty)
        {
            return Result.Failure(Error.Validation("PresentacionVenta.UnidadVentaId", "Unidad de Venta is required."));
        }

        if (string.IsNullOrWhiteSpace(descripcion))
        {
            return Result.Failure(Error.Validation("PresentacionVenta.Descripcion", "Descripcion is required."));
        }

        if (descripcion.Length > 100)
        {
            return Result.Failure(Error.Validation("PresentacionVenta.Descripcion", "Descripcion cannot exceed 100 characters."));
        }

        if (cantidadUnidadesBase <= 0)
        {
            return Result.Failure(Error.Validation("PresentacionVenta.CantidadUnidadesBase", "Cantidad Unidades Base must be greater than zero."));
        }

        if (precioVenta < 0)
        {
            return Result.Failure(Error.Validation("PresentacionVenta.PrecioVenta", "Precio Venta cannot be negative."));
        }

        UnidadVentaId = unidadVentaId;
        Descripcion = descripcion;
        CantidadUnidadesBase = cantidadUnidadesBase;
        PrecioVenta = precioVenta;

        return Result.Success();
    }
}
