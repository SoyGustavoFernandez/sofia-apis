-- ==============================================================================
-- 0003: FIX LOTES DATES - CONVERT DATE TO DATETIMEOFFSET
-- ==============================================================================

-- Drop index that depends on Fecha_Caducidad
DROP INDEX IX_Lotes_Caducidad ON Lotes_Inventario;

-- Alter Fecha_Fabricacion
ALTER TABLE Lotes_Inventario 
ALTER COLUMN Fecha_Fabricacion DATETIMEOFFSET NULL;

-- Alter Fecha_Caducidad
ALTER TABLE Lotes_Inventario 
ALTER COLUMN Fecha_Caducidad DATETIMEOFFSET NOT NULL;

-- Recreate index
CREATE NONCLUSTERED INDEX IX_Lotes_Caducidad ON Lotes_Inventario(Fecha_Caducidad);
