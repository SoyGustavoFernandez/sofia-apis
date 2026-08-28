-- ==============================================================================
-- 0005: EMPRESA - MULTI-TENANT SUPPORT
-- Creates the Empresas table and adds multi-tenant columns to existing tables
-- ==============================================================================

-- 1. TABLE: Empresas
-- ------------------------------------------------------------------------------
CREATE TABLE Empresas (
    Empresa_ID          UNIQUEIDENTIFIER    NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Nombre              NVARCHAR(200)       NOT NULL,
    RUC                 CHAR(11)            NULL,
    Estado              INT                 NOT NULL DEFAULT 0,
    FechaInicioTrial    DATETIMEOFFSET      NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    FechaVencimiento    DATETIMEOFFSET      NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    TenantId            UNIQUEIDENTIFIER    NULL,
    CreatedAt           DATETIMEOFFSET      NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy           NVARCHAR(100)       NULL,
    LastModifiedAt      DATETIMEOFFSET      NULL,
    LastModifiedBy      NVARCHAR(100)       NULL,
    IsDeleted           BIT                 NOT NULL DEFAULT 0,
    DeletedAt           DATETIMEOFFSET      NULL,
    DeletedBy           NVARCHAR(100)       NULL
);

-- Filtered unique index on RUC (NULLs allowed, only non-NULL values must be unique)
CREATE UNIQUE INDEX IX_Empresas_RUC
    ON Empresas (RUC)
    WHERE RUC IS NOT NULL;


-- 2. Sucursales: add Empresa_ID FK and TenantId
-- ------------------------------------------------------------------------------
ALTER TABLE Sucursales
    ADD Empresa_ID  UNIQUEIDENTIFIER    NULL,
        TenantId    UNIQUEIDENTIFIER    NULL;

ALTER TABLE Sucursales
    ADD CONSTRAINT FK_Sucursales_Empresa
        FOREIGN KEY (Empresa_ID) REFERENCES Empresas(Empresa_ID);


-- 3. Empleados: add TenantId
-- ------------------------------------------------------------------------------
ALTER TABLE Empleados
    ADD TenantId UNIQUEIDENTIFIER NULL;


-- 4. Seguridad_Cuentas: add TenantId
-- ------------------------------------------------------------------------------
ALTER TABLE Seguridad_Cuentas
    ADD TenantId UNIQUEIDENTIFIER NULL;


-- 5. TenantId en todas las tablas de entidades restantes (detección por IsDeleted)
-- ------------------------------------------------------------------------------
DECLARE @sql NVARCHAR(MAX) = N'';

SELECT @sql += N'ALTER TABLE [' + t.name + N'] ADD TenantId UNIQUEIDENTIFIER NULL;' + CHAR(13)
FROM sys.tables t
WHERE NOT EXISTS (
    SELECT 1 FROM sys.columns c
    WHERE c.object_id = t.object_id AND c.name = 'TenantId'
)
AND EXISTS (
    SELECT 1 FROM sys.columns c2
    WHERE c2.object_id = t.object_id AND c2.name = 'IsDeleted'
);

IF LEN(@sql) > 0
    EXEC sp_executesql @sql;


-- 6. Sucursales: índice único compuesto por empresa (en lugar de global)
-- ------------------------------------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes i
    JOIN sys.tables t ON i.object_id = t.object_id
    WHERE t.name = 'Sucursales' AND i.name = 'UX_Sucursales_Empresa_Licencia'
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_Sucursales_Empresa_Licencia
        ON Sucursales (Empresa_ID, Numero_Licencia)
        WHERE IsDeleted = 0 AND Empresa_ID IS NOT NULL;
END
