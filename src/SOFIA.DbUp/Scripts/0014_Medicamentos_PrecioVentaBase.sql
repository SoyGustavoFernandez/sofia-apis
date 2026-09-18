-- Reference price for selling a product loose, by its own base unit (no Presentacion de
-- Venta). Nullable because many Medicamentos already exist without one on file; existing
-- rows keep it unset until someone edits them, but Create/Update now always require a value.
ALTER TABLE Medicamentos ADD Precio_Venta_Base DECIMAL(10,2) NULL;
GO
