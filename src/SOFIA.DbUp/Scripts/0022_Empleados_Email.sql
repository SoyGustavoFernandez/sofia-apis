-- Optional employee email, used as the destination of password recovery links and unique per company when present
IF COL_LENGTH('Empleados', 'Email') IS NULL
    ALTER TABLE Empleados ADD Email VARCHAR(254) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Empleados_Tenant_Email' AND object_id = OBJECT_ID('Empleados'))
    CREATE UNIQUE INDEX UX_Empleados_Tenant_Email
        ON Empleados (TenantId, Email)
        WHERE Email IS NOT NULL AND IsDeleted = 0;
GO
