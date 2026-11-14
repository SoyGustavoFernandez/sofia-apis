-- Platform operator flag (set by hand, never by the application) and the single-row payment details shown to companies that must pay
IF COL_LENGTH('Seguridad_Cuentas', 'Es_Admin_Plataforma') IS NULL
    ALTER TABLE Seguridad_Cuentas ADD Es_Admin_Plataforma BIT NOT NULL CONSTRAINT DF_Seguridad_Cuentas_Es_Admin_Plataforma DEFAULT 0;
GO

IF OBJECT_ID('Plataforma_Configuracion_Pago', 'U') IS NULL
    CREATE TABLE [Plataforma_Configuracion_Pago] (
        [Configuracion_ID] UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Plataforma_Configuracion_Pago_ID DEFAULT NEWSEQUENTIALID() CONSTRAINT PK_Plataforma_Configuracion_Pago PRIMARY KEY,
        [Whatsapp_Numero] VARCHAR(15) NOT NULL,
        [Yape_Numero] VARCHAR(9) NULL,
        [Instrucciones] NVARCHAR(500) NULL,
        [Qr_Imagen] VARBINARY(MAX) NULL,
        [Qr_Content_Type] VARCHAR(20) NULL,
        [TenantId] UNIQUEIDENTIFIER NULL,
        [CreatedAt] DATETIMEOFFSET NOT NULL CONSTRAINT DF_Plataforma_Configuracion_Pago_CreatedAt DEFAULT SYSDATETIMEOFFSET(),
        [CreatedBy] NVARCHAR(100) NULL,
        [LastModifiedAt] DATETIMEOFFSET NULL,
        [LastModifiedBy] NVARCHAR(100) NULL,
        [IsDeleted] BIT NOT NULL CONSTRAINT DF_Plataforma_Configuracion_Pago_IsDeleted DEFAULT 0,
        [DeletedAt] DATETIMEOFFSET NULL,
        [DeletedBy] NVARCHAR(100) NULL
    );
GO
