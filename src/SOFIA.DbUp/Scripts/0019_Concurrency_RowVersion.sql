-- Optimistic concurrency: rowversion on rows written through read-modify-write, and one open cash session per cashier
IF COL_LENGTH('Inventario_Sucursal', 'RowVersion') IS NULL
    ALTER TABLE Inventario_Sucursal ADD RowVersion ROWVERSION NOT NULL;
GO

IF COL_LENGTH('Ventas_Cabecera', 'RowVersion') IS NULL
    ALTER TABLE Ventas_Cabecera ADD RowVersion ROWVERSION NOT NULL;
GO

IF COL_LENGTH('Transferencias_Cab', 'RowVersion') IS NULL
    ALTER TABLE Transferencias_Cab ADD RowVersion ROWVERSION NOT NULL;
GO

IF COL_LENGTH('POS_Sesiones_Caja', 'RowVersion') IS NULL
    ALTER TABLE POS_Sesiones_Caja ADD RowVersion ROWVERSION NOT NULL;
GO

IF COL_LENGTH('RefreshTokens', 'RowVersion') IS NULL
    ALTER TABLE RefreshTokens ADD RowVersion ROWVERSION NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_POS_Sesiones_Caja_Empleado_Abierta' AND object_id = OBJECT_ID('POS_Sesiones_Caja'))
    CREATE UNIQUE INDEX UX_POS_Sesiones_Caja_Empleado_Abierta
        ON POS_Sesiones_Caja (Empleado_ID)
        WHERE Estado_Sesion = 'Abierta' AND IsDeleted = 0;
GO
