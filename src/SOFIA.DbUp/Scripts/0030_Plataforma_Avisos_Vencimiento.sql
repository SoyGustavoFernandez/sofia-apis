-- Remembers which expiry notices already went out, so a retried or repeated daily run never mails twice
IF OBJECT_ID('Plataforma_Avisos_Enviados', 'U') IS NULL
    CREATE TABLE [Plataforma_Avisos_Enviados] (
        [Aviso_ID] UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Plataforma_Avisos_Enviados_ID DEFAULT NEWSEQUENTIALID() CONSTRAINT PK_Plataforma_Avisos_Enviados PRIMARY KEY,
        [Empresa_ID] UNIQUEIDENTIFIER NULL,
        [Fecha] DATE NOT NULL,
        [Dias] INT NOT NULL,
        [TenantId] UNIQUEIDENTIFIER NULL,
        [CreatedAt] DATETIMEOFFSET NOT NULL CONSTRAINT DF_Plataforma_Avisos_Enviados_CreatedAt DEFAULT SYSDATETIMEOFFSET(),
        [CreatedBy] NVARCHAR(100) NULL,
        [LastModifiedAt] DATETIMEOFFSET NULL,
        [LastModifiedBy] NVARCHAR(100) NULL,
        [IsDeleted] BIT NOT NULL CONSTRAINT DF_Plataforma_Avisos_Enviados_IsDeleted DEFAULT 0,
        [DeletedAt] DATETIMEOFFSET NULL,
        [DeletedBy] NVARCHAR(100) NULL
    );
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Plataforma_Avisos_Enviados' AND object_id = OBJECT_ID('Plataforma_Avisos_Enviados'))
    CREATE UNIQUE INDEX UX_Plataforma_Avisos_Enviados ON Plataforma_Avisos_Enviados (Empresa_ID, Fecha, Dias);
GO
