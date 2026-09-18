-- Presentaciones de Venta now derive their base-unit equivalence from the product's own
-- Jerarquía de Unidades (see JerarquiaConversionResolver) instead of a hand-typed quantity,
-- so every presentación is now tied to the specific UnidadMedida it sells (e.g. "Caja").
--
-- The two rows that existed before this change were manual test data whose quantity was
-- typed by hand rather than resolved from a Jerarquía; they cannot be backfilled with a
-- real Unidad_Venta_ID, so they are removed rather than left permanently invalid.
DELETE FROM Presentaciones_Venta;
GO

ALTER TABLE Presentaciones_Venta ADD Unidad_Venta_ID UNIQUEIDENTIFIER NOT NULL;
GO

ALTER TABLE Presentaciones_Venta ADD CONSTRAINT FK_PresentacionesVenta_UnidadVenta FOREIGN KEY (Unidad_Venta_ID) REFERENCES Unidades_Medida(UoM_ID);
GO
