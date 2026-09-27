-- Returns record the cash session, amount and method of the customer refund so the arqueo can subtract cash refunds
ALTER TABLE Devoluciones_Cabecera
    ADD Sesion_ID UNIQUEIDENTIFIER NULL,
        Monto_Reembolsado DECIMAL(12,2) NULL,
        Metodo_Reembolso VARCHAR(50) NULL;
GO

ALTER TABLE Devoluciones_Cabecera
    ADD CONSTRAINT FK_Devolucion_Sesion FOREIGN KEY (Sesion_ID) REFERENCES POS_Sesiones_Caja(Sesion_ID);
GO

CREATE NONCLUSTERED INDEX IX_Devoluciones_Sesion ON Devoluciones_Cabecera(Sesion_ID) WHERE Sesion_ID IS NOT NULL AND IsDeleted = 0;
GO
