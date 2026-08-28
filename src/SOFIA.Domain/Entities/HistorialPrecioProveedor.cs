using SOFIA.Domain.Common;

namespace SOFIA.Domain.Entities;

public sealed class HistorialPrecioProveedor : BaseEntity
{
    private HistorialPrecioProveedor() { } // Required for EF Core

    public Guid ProveedorId { get; private set; }
    public Guid ProductoId { get; private set; }
    public decimal CostoPorUnidadBase { get; private set; }
    public DateTime FechaInicioVigencia { get; private set; }
    public DateTime? FechaFinVigencia { get; private set; }
    public int LeadTimeDias { get; private set; }
    public int CantidadMinCompra { get; private set; }

    // Navigation Properties
    public ProveedorDistribuidor? Proveedor { get; }
    public Medicamento? Producto { get; }

    public static Result<HistorialPrecioProveedor> Create(
        Guid proveedorId,
        Guid productoId,
        decimal costoPorUnidadBase,
        DateTime fechaInicioVigencia,
        DateTime? fechaFinVigencia,
        int leadTimeDias,
        int cantidadMinCompra = 1)
    {
        if (proveedorId == Guid.Empty)
        {
            return Result.Failure<HistorialPrecioProveedor>(Error.Validation("HistorialPrecioProveedor.ProveedorId", "Proveedor ID is required."));
        }

        if (productoId == Guid.Empty)
        {
            return Result.Failure<HistorialPrecioProveedor>(Error.Validation("HistorialPrecioProveedor.ProductoId", "Producto ID is required."));
        }

        if (costoPorUnidadBase < 0.0m)
        {
            return Result.Failure<HistorialPrecioProveedor>(Error.Validation("HistorialPrecioProveedor.CostoPorUnidadBase", "Costo por Unidad Base must be greater than or equal to 0."));
        }

        if (fechaInicioVigencia == default)
        {
            return Result.Failure<HistorialPrecioProveedor>(Error.Validation("HistorialPrecioProveedor.FechaInicioVigencia", "Fecha de Inicio de Vigencia is required."));
        }

        if (fechaFinVigencia.HasValue && fechaFinVigencia.Value < fechaInicioVigencia)
        {
            return Result.Failure<HistorialPrecioProveedor>(Error.Validation("HistorialPrecioProveedor.FechaFinVigencia", "Fecha Fin de Vigencia cannot be earlier than Fecha Inicio de Vigencia."));
        }

        if (leadTimeDias < 0)
        {
            return Result.Failure<HistorialPrecioProveedor>(Error.Validation("HistorialPrecioProveedor.LeadTimeDias", "Lead Time Days must be greater than or equal to 0."));
        }

        if (cantidadMinCompra < 1)
        {
            return Result.Failure<HistorialPrecioProveedor>(Error.Validation("HistorialPrecioProveedor.CantidadMinCompra", "Cantidad Mínima de Compra must be at least 1."));
        }

        return Result.Success(new HistorialPrecioProveedor
        {
            ProveedorId = proveedorId,
            ProductoId = productoId,
            CostoPorUnidadBase = costoPorUnidadBase,
            FechaInicioVigencia = fechaInicioVigencia,
            FechaFinVigencia = fechaFinVigencia,
            LeadTimeDias = leadTimeDias,
            CantidadMinCompra = cantidadMinCompra
        });
    }

    public Result Update(
        Guid proveedorId,
        Guid productoId,
        decimal costoPorUnidadBase,
        DateTime fechaInicioVigencia,
        DateTime? fechaFinVigencia,
        int leadTimeDias,
        int cantidadMinCompra)
    {
        if (proveedorId == Guid.Empty)
        {
            return Result.Failure(Error.Validation("HistorialPrecioProveedor.ProveedorId", "Proveedor ID is required."));
        }

        if (productoId == Guid.Empty)
        {
            return Result.Failure(Error.Validation("HistorialPrecioProveedor.ProductoId", "Producto ID is required."));
        }

        if (costoPorUnidadBase < 0.0m)
        {
            return Result.Failure(Error.Validation("HistorialPrecioProveedor.CostoPorUnidadBase", "Costo por Unidad Base must be greater than or equal to 0."));
        }

        if (fechaInicioVigencia == default)
        {
            return Result.Failure(Error.Validation("HistorialPrecioProveedor.FechaInicioVigencia", "Fecha de Inicio de Vigencia is required."));
        }

        if (fechaFinVigencia.HasValue && fechaFinVigencia.Value < fechaInicioVigencia)
        {
            return Result.Failure(Error.Validation("HistorialPrecioProveedor.FechaFinVigencia", "Fecha Fin de Vigencia cannot be earlier than Fecha Inicio de Vigencia."));
        }

        if (leadTimeDias < 0)
        {
            return Result.Failure(Error.Validation("HistorialPrecioProveedor.LeadTimeDias", "Lead Time Days must be greater than or equal to 0."));
        }

        if (cantidadMinCompra < 1)
        {
            return Result.Failure(Error.Validation("HistorialPrecioProveedor.CantidadMinCompra", "Cantidad Mínima de Compra must be at least 1."));
        }

        ProveedorId = proveedorId;
        ProductoId = productoId;
        CostoPorUnidadBase = costoPorUnidadBase;
        FechaInicioVigencia = fechaInicioVigencia;
        FechaFinVigencia = fechaFinVigencia;
        LeadTimeDias = leadTimeDias;
        CantidadMinCompra = cantidadMinCompra;

        return Result.Success();
    }
}
