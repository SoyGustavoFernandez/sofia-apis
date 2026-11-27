-- Service appointments of each branch
IF OBJECT_ID('Servicios_Citas', 'U') IS NULL
    CREATE TABLE [Servicios_Citas] (
        [Cita_ID] UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Servicios_Citas_ID DEFAULT NEWSEQUENTIALID() CONSTRAINT PK_Servicios_Citas PRIMARY KEY,
        [Cliente_ID] UNIQUEIDENTIFIER NOT NULL CONSTRAINT FK_Servicios_Citas_Cliente REFERENCES Pacientes_Clientes(Cliente_ID),
        [Servicio_ID] UNIQUEIDENTIFIER NOT NULL CONSTRAINT FK_Servicios_Citas_Servicio REFERENCES Servicios_Catalogo(Servicio_ID),
        [Sucursal_ID] UNIQUEIDENTIFIER NOT NULL,
        [FechaHora] DATETIMEOFFSET NOT NULL,
        [Estado] INT NOT NULL,
        [AceptaRecordatorio] BIT NOT NULL CONSTRAINT DF_Servicios_Citas_Acepta DEFAULT 0,
        [TenantId] UNIQUEIDENTIFIER NULL,
        [CreatedAt] DATETIMEOFFSET NOT NULL CONSTRAINT DF_Servicios_Citas_CreatedAt DEFAULT SYSDATETIMEOFFSET(),
        [CreatedBy] NVARCHAR(100) NULL,
        [LastModifiedAt] DATETIMEOFFSET NULL,
        [LastModifiedBy] NVARCHAR(100) NULL,
        [IsDeleted] BIT NOT NULL CONSTRAINT DF_Servicios_Citas_IsDeleted DEFAULT 0,
        [DeletedAt] DATETIMEOFFSET NULL,
        [DeletedBy] NVARCHAR(100) NULL
    );
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Servicios_Citas_Sucursal_Fecha' AND object_id = OBJECT_ID('Servicios_Citas'))
    CREATE INDEX IX_Servicios_Citas_Sucursal_Fecha ON Servicios_Citas (TenantId, Sucursal_ID, FechaHora) WHERE IsDeleted = 0;
GO
