-- Adds "Presentaciones de Venta" (sale presentations): per-product ways to sell it
-- (e.g. "Unidad", "Caja x10"), each with its own commercial price (not derived from
-- the base unit price) and its equivalence in base units, so the correct amount of
-- stock is discounted regardless of which presentation was sold.
CREATE TABLE [Presentaciones_Venta] (
    [Presentacion_ID] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    [Producto_ID] UNIQUEIDENTIFIER NOT NULL,
    [Descripcion] NVARCHAR(100) NOT NULL,
    [Cantidad_Unidades_Base] DECIMAL(12,4) NOT NULL,
    [Precio_Venta] DECIMAL(10,2) NOT NULL,
    [CreatedAt] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [CreatedBy] NVARCHAR(100) NULL,
    [LastModifiedAt] DATETIMEOFFSET NULL,
    [LastModifiedBy] NVARCHAR(100) NULL,
    [IsDeleted] BIT NOT NULL DEFAULT 0,
    [DeletedAt] DATETIMEOFFSET NULL,
    [DeletedBy] NVARCHAR(100) NULL,
    CONSTRAINT [FK_PresentacionesVenta_Producto] FOREIGN KEY ([Producto_ID]) REFERENCES [Medicamentos]([Producto_ID])
);
GO

CREATE UNIQUE NONCLUSTERED INDEX UX_PresentacionesVenta_Producto_Descripcion ON Presentaciones_Venta(Producto_ID, Descripcion) WHERE IsDeleted = 0;
GO

-- Nullable so existing sales (and any sale made without picking a presentation) are
-- unaffected: Ventas_Detalle.Cantidad_Vendida keeps meaning "base units" either way.
ALTER TABLE Ventas_Detalle ADD Presentacion_ID UNIQUEIDENTIFIER NULL;
GO

ALTER TABLE Ventas_Detalle ADD Cantidad_En_Presentacion DECIMAL(12,4) NULL;
GO

ALTER TABLE Ventas_Detalle ADD CONSTRAINT FK_Detalle_Presentacion FOREIGN KEY (Presentacion_ID) REFERENCES Presentaciones_Venta(Presentacion_ID);
GO
