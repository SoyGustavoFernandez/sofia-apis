-- Payments are always read by sale (receipt, cash count, dashboard), which otherwise scans the whole table
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Ventas_Pagos_Transaccion' AND object_id = OBJECT_ID('Ventas_Pagos'))
    CREATE INDEX IX_Ventas_Pagos_Transaccion
        ON Ventas_Pagos (Transaccion_ID);
GO
