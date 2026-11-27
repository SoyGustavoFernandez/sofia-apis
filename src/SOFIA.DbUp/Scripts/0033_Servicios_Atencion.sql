-- What is recorded when a service is performed; a walk-in may have no patient
ALTER TABLE Servicios_Citas ALTER COLUMN Cliente_ID UNIQUEIDENTIFIER NULL;
GO

IF COL_LENGTH('Servicios_Citas', 'VentaId') IS NULL
    ALTER TABLE Servicios_Citas ADD
        [Origen] INT NULL,
        [Lote_ID] UNIQUEIDENTIFIER NULL,
        [MedicamentoTraido] NVARCHAR(200) NULL,
        [Dosis] NVARCHAR(200) NULL,
        [Resultado] NVARCHAR(200) NULL,
        [VentaPrevia_ID] UNIQUEIDENTIFIER NULL,
        [VentaId] UNIQUEIDENTIFIER NULL,
        [FechaAtencion] DATETIMEOFFSET NULL;
GO

-- Services charged in a sale
IF OBJECT_ID('Ventas_Servicios', 'U') IS NULL
    CREATE TABLE [Ventas_Servicios] (
        [VentaServicio_ID] UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Ventas_Servicios_ID DEFAULT NEWSEQUENTIALID() CONSTRAINT PK_Ventas_Servicios PRIMARY KEY,
        [Transaccion_ID] UNIQUEIDENTIFIER NOT NULL CONSTRAINT FK_Ventas_Servicios_Venta REFERENCES Ventas_Cabecera(Transaccion_ID),
        [Servicio_ID] UNIQUEIDENTIFIER NOT NULL CONSTRAINT FK_Ventas_Servicios_Servicio REFERENCES Servicios_Catalogo(Servicio_ID),
        [Cita_ID] UNIQUEIDENTIFIER NOT NULL CONSTRAINT FK_Ventas_Servicios_Cita REFERENCES Servicios_Citas(Cita_ID),
        [Precio] DECIMAL(10,2) NOT NULL CONSTRAINT CHK_Ventas_Servicios_Precio CHECK ([Precio] > 0),
        [TenantId] UNIQUEIDENTIFIER NULL,
        [CreatedAt] DATETIMEOFFSET NOT NULL CONSTRAINT DF_Ventas_Servicios_CreatedAt DEFAULT SYSDATETIMEOFFSET(),
        [CreatedBy] NVARCHAR(100) NULL,
        [LastModifiedAt] DATETIMEOFFSET NULL,
        [LastModifiedBy] NVARCHAR(100) NULL,
        [IsDeleted] BIT NOT NULL CONSTRAINT DF_Ventas_Servicios_IsDeleted DEFAULT 0,
        [DeletedAt] DATETIMEOFFSET NULL,
        [DeletedBy] NVARCHAR(100) NULL
    );
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Ventas_Servicios_Transaccion' AND object_id = OBJECT_ID('Ventas_Servicios'))
    CREATE INDEX IX_Ventas_Servicios_Transaccion ON Ventas_Servicios (Transaccion_ID);
GO
