-- Services each company charges for; nothing is preloaded and names are unique per company
IF OBJECT_ID('Servicios_Catalogo', 'U') IS NULL
    CREATE TABLE [Servicios_Catalogo] (
        [Servicio_ID] UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Servicios_Catalogo_ID DEFAULT NEWSEQUENTIALID() CONSTRAINT PK_Servicios_Catalogo PRIMARY KEY,
        [Nombre] NVARCHAR(150) NOT NULL,
        [Precio] DECIMAL(10,2) NOT NULL CONSTRAINT CK_Servicios_Catalogo_Precio CHECK ([Precio] > 0),
        [AplicaMedicamento] BIT NOT NULL CONSTRAINT DF_Servicios_Catalogo_Aplica DEFAULT 0,
        [RegistraResultado] BIT NOT NULL CONSTRAINT DF_Servicios_Catalogo_Resultado DEFAULT 0,
        [TenantId] UNIQUEIDENTIFIER NULL,
        [CreatedAt] DATETIMEOFFSET NOT NULL CONSTRAINT DF_Servicios_Catalogo_CreatedAt DEFAULT SYSDATETIMEOFFSET(),
        [CreatedBy] NVARCHAR(100) NULL,
        [LastModifiedAt] DATETIMEOFFSET NULL,
        [LastModifiedBy] NVARCHAR(100) NULL,
        [IsDeleted] BIT NOT NULL CONSTRAINT DF_Servicios_Catalogo_IsDeleted DEFAULT 0,
        [DeletedAt] DATETIMEOFFSET NULL,
        [DeletedBy] NVARCHAR(100) NULL
    );
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Servicios_Catalogo_Tenant_Nombre' AND object_id = OBJECT_ID('Servicios_Catalogo'))
    CREATE UNIQUE INDEX UX_Servicios_Catalogo_Tenant_Nombre ON Servicios_Catalogo (TenantId, Nombre) WHERE IsDeleted = 0;
GO
