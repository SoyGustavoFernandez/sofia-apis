-- Fiscal series become a per-branch master: Proforma type, unique prefixes, one active series per type and unique fiscal numbers
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_Tipo_Comprobante' AND parent_object_id = OBJECT_ID('SUNAT_Series_Fiscales'))
    ALTER TABLE SUNAT_Series_Fiscales DROP CONSTRAINT CHK_Tipo_Comprobante;
GO

ALTER TABLE SUNAT_Series_Fiscales
    ADD CONSTRAINT CHK_Tipo_Comprobante CHECK (Tipo_Comprobante IN ('00', '01', '03', '07', '08', 'PR'));
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_SUNAT_Series_Fiscales_Tenant_Tipo_Prefijo' AND object_id = OBJECT_ID('SUNAT_Series_Fiscales'))
    CREATE UNIQUE INDEX UX_SUNAT_Series_Fiscales_Tenant_Tipo_Prefijo
        ON SUNAT_Series_Fiscales (TenantId, Tipo_Comprobante, Prefijo_Serie)
        WHERE IsDeleted = 0;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_SUNAT_Series_Fiscales_Sucursal_Tipo_Activa' AND object_id = OBJECT_ID('SUNAT_Series_Fiscales'))
    CREATE UNIQUE INDEX UX_SUNAT_Series_Fiscales_Sucursal_Tipo_Activa
        ON SUNAT_Series_Fiscales (Sucursal_ID, Tipo_Comprobante)
        WHERE Estado_Serie = 'Activa' AND IsDeleted = 0;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_SUNAT_Comprobantes_Emitidos_Serie_Correlativo' AND object_id = OBJECT_ID('SUNAT_Comprobantes_Emitidos'))
    CREATE UNIQUE INDEX UX_SUNAT_Comprobantes_Emitidos_Serie_Correlativo
        ON SUNAT_Comprobantes_Emitidos (Serie_ID, Numero_Correlativo);
GO

-- Declared by the EF model since the start but never created by a script
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Inventario_Sucursal_Lote' AND object_id = OBJECT_ID('Inventario_Sucursal'))
    CREATE UNIQUE INDEX UX_Inventario_Sucursal_Lote
        ON Inventario_Sucursal (Sucursal_ID, Lote_ID)
        WHERE IsDeleted = 0;
GO
