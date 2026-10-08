-- Cash session from which a charged sale was voided, so the cash handed back is counted in the drawer it left
IF COL_LENGTH('Ventas_Cabecera', 'Sesion_Anulacion_ID') IS NULL
    ALTER TABLE Ventas_Cabecera ADD Sesion_Anulacion_ID UNIQUEIDENTIFIER NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_VentaCab_Sesion_Anulacion')
    ALTER TABLE Ventas_Cabecera ADD CONSTRAINT FK_VentaCab_Sesion_Anulacion FOREIGN KEY (Sesion_Anulacion_ID) REFERENCES POS_Sesiones_Caja(Sesion_ID);
GO
