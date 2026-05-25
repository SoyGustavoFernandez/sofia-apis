CREATE TABLE [DIGEMID_Catalogo_Productos] (
    [Catalogo_ID] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    [Cod_Prod] NVARCHAR(20) NOT NULL UNIQUE,
    [Nom_Prod] NVARCHAR(255) NOT NULL,
    [Concent] NVARCHAR(255) NULL,
    [Forma_Farmaceutica] NVARCHAR(150) NULL,
    [Fraccion] NVARCHAR(100) NULL,
    [Registro_Sanitario] NVARCHAR(50) NULL,
    [Titular] NVARCHAR(255) NULL,
    [Estado] NVARCHAR(50) NOT NULL,
    
    -- Campos Auditables
    [CreatedBy] NVARCHAR(100) NOT NULL,
    [CreatedAt] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [LastModifiedBy] NVARCHAR(100) NULL,
    [LastModifiedAt] DATETIMEOFFSET NULL,
    [DeletedBy] NVARCHAR(100) NULL,
    [DeletedAt] DATETIMEOFFSET NULL,
    [IsDeleted] BIT NOT NULL DEFAULT 0
);
GO

CREATE INDEX [IX_DIGEMID_Catalogo_Productos_Nom_Prod] ON [DIGEMID_Catalogo_Productos] ([Nom_Prod]);
GO
