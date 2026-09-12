-- Replaces the free-text Unidad_Dosis_Clinica column on Formulacion_Clinica with a
-- proper FK to Unidades_Medida, so dosage units are catalog-backed and autocompletable.

ALTER TABLE Formulacion_Clinica ADD Unidad_Medida_ID UNIQUEIDENTIFIER NULL;
GO

-- Best-effort backfill for any pre-existing rows: match the legacy free-text unit
-- against the unit-of-measure catalog by code (case-insensitive default collation).
UPDATE fc
SET fc.Unidad_Medida_ID = um.UoM_ID
FROM Formulacion_Clinica fc
INNER JOIN Unidades_Medida um ON um.Codigo_UoM = fc.Unidad_Dosis_Clinica
WHERE fc.Unidad_Medida_ID IS NULL;
GO

-- Stop rather than silently drop data if a legacy unit could not be matched.
IF EXISTS (SELECT 1 FROM Formulacion_Clinica WHERE Unidad_Medida_ID IS NULL)
BEGIN
    RAISERROR('Formulacion_Clinica has rows whose Unidad_Dosis_Clinica could not be matched to Unidades_Medida.Codigo_UoM. Resolve manually before re-running this migration.', 16, 1);
END
GO

ALTER TABLE Formulacion_Clinica DROP COLUMN Unidad_Dosis_Clinica;
GO

ALTER TABLE Formulacion_Clinica ALTER COLUMN Unidad_Medida_ID UNIQUEIDENTIFIER NOT NULL;
GO

ALTER TABLE Formulacion_Clinica ADD CONSTRAINT FK_Formulacion_UnidadMedida FOREIGN KEY (Unidad_Medida_ID) REFERENCES Unidades_Medida(UoM_ID);
GO
