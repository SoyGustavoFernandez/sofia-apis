-- Sale lines and fiscal receipts are always read by sale, which otherwise scans both tables in full
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Ventas_Detalle_Transaccion' AND object_id = OBJECT_ID('Ventas_Detalle'))
    CREATE INDEX IX_Ventas_Detalle_Transaccion
        ON Ventas_Detalle (Transaccion_ID);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SUNAT_Comprobantes_Emitidos_Transaccion' AND object_id = OBJECT_ID('SUNAT_Comprobantes_Emitidos'))
    CREATE INDEX IX_SUNAT_Comprobantes_Emitidos_Transaccion
        ON SUNAT_Comprobantes_Emitidos (Transaccion_ID);
GO
