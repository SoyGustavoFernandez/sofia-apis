-- A sale has at most one live delivery dispatch
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Despachos_Delivery_Transaccion' AND object_id = OBJECT_ID('Despachos_Delivery'))
    CREATE UNIQUE INDEX UX_Despachos_Delivery_Transaccion
        ON Despachos_Delivery (Transaccion_ID)
        WHERE IsDeleted = 0;
GO
