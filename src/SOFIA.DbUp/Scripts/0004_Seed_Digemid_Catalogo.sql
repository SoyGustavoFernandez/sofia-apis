-- Migrar datos desde la base de datos origen [SofiaDb] hacia nuestra nueva tabla [DIGEMID_Catalogo_Productos]
-- Solo ejecutamos la inserción si la tabla de origen existe y la tabla destino está vacía
IF EXISTS (SELECT 1 FROM sys.databases WHERE name = 'SofiaDb')
BEGIN
    IF NOT EXISTS (SELECT 1 FROM [DIGEMID_Catalogo_Productos])
    BEGIN
        INSERT INTO [DIGEMID_Catalogo_Productos] (
            [Catalogo_ID],
            [Cod_Prod],
            [Nom_Prod],
            [Concent],
            [Forma_Farmaceutica],
            [Fraccion],
            [Registro_Sanitario],
            [Titular],
            [Estado],
            [CreatedBy],
            [CreatedAt],
            [IsDeleted]
        )
        SELECT 
            NEWID(),
            ISNULL([Cod_Prod], 'UNKNOWN-' + CAST(NEWID() AS NVARCHAR(36))),
            ISNULL([Nom_Prod], 'SIN NOMBRE'),
            [Concent],
            NULL, -- [Forma_Farm]
            NULL, -- [Fra_Accion]
            NULL, -- [Reg_Sanitario]
            NULL, -- [Titular]
            'Activo', -- [Estado]
            'SYSTEM-MIGRATION',
            SYSDATETIMEOFFSET(),
            0
        FROM [SofiaDb].[dbo].[DigemidCatalogo];
        
        PRINT 'Datos del Catálogo DIGEMID copiados exitosamente desde SofiaDb.';
    END
    ELSE
    BEGIN
        PRINT 'La tabla DIGEMID_Catalogo_Productos ya contiene datos. Se omite la migración.';
    END
END
ELSE
BEGIN
    PRINT 'La base de datos SofiaDb no existe en este servidor. No se pudo migrar el catálogo de DIGEMID.';
END
GO
