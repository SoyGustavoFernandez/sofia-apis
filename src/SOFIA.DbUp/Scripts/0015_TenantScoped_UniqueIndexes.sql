-- Every entity is now isolated per company, so business keys must be unique per tenant, not globally.
-- Original constraint names were auto-generated (UQ__...), so they are resolved from the catalog by key column.
-- Global on purpose (untouched): Empresas.RUC, Seguridad_Cuentas.Nombre_Usuario, RefreshTokens.TokenHash.
DECLARE @targets TABLE (TableName SYSNAME, ColumnName SYSNAME);
INSERT INTO @targets (TableName, ColumnName) VALUES
    (N'Aseguradoras_Medicas', N'Codigo_Identificador_Nacional'),
    (N'DIGEMID_Actas_Destruccion', N'Numero_Resolucion_Interna'),
    (N'DIGEMID_Catalogo_Productos', N'Cod_Prod'),
    (N'Empleados', N'Licencia_Prof'),
    (N'Ingredientes_Activos', N'Denominacion_DCI'),
    (N'Laboratorios', N'Codigo_Identificador'),
    (N'Laboratorios', N'Nombre_Compania'),
    (N'Medicamentos', N'Codigo_Nacional'),
    (N'Pacientes_Clientes', N'Doc_Identidad_Gub'),
    (N'Profesionales_Salud', N'Numero_Registro'),
    (N'Proveedores_Dist', N'Tax_ID'),
    (N'Proveedores_Dist', N'Razon_Social'),
    (N'Seguridad_Roles', N'Nombre_Rol'),
    (N'Unidades_Medida', N'Codigo_UoM');

DECLARE @table SYSNAME, @column SYSNAME, @index SYSNAME, @isConstraint BIT, @filter NVARCHAR(MAX), @newIndex SYSNAME, @sql NVARCHAR(MAX);

DECLARE target_cursor CURSOR LOCAL FAST_FORWARD FOR SELECT TableName, ColumnName FROM @targets;
OPEN target_cursor;
FETCH NEXT FROM target_cursor INTO @table, @column;

WHILE @@FETCH_STATUS = 0
BEGIN
    SELECT @index = NULL, @isConstraint = NULL, @filter = NULL;
    SET @newIndex = N'UX_' + @table + N'_Tenant_' + @column;

    -- Single-column unique index/constraint on exactly this key column
    SELECT TOP 1 @index = i.name, @isConstraint = i.is_unique_constraint, @filter = i.filter_definition
    FROM sys.indexes i
    WHERE i.object_id = OBJECT_ID(@table)
      AND i.is_unique = 1
      AND i.is_primary_key = 0
      AND i.name <> @newIndex
      AND (SELECT COUNT(*) FROM sys.index_columns ic
           WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id AND ic.is_included_column = 0) = 1
      AND EXISTS (SELECT 1 FROM sys.index_columns ic
                  JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                  WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id
                    AND ic.is_included_column = 0 AND c.name = @column);

    IF @index IS NOT NULL
    BEGIN
        SET @sql = CASE WHEN @isConstraint = 1
            THEN N'ALTER TABLE ' + QUOTENAME(@table) + N' DROP CONSTRAINT ' + QUOTENAME(@index)
            ELSE N'DROP INDEX ' + QUOTENAME(@index) + N' ON ' + QUOTENAME(@table) END;
        EXEC sp_executesql @sql;
    END

    -- Keeps the original filter (e.g. IsDeleted = 0) so uniqueness semantics only gain the tenant dimension
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(@table) AND name = @newIndex)
    BEGIN
        SET @sql = N'CREATE UNIQUE INDEX ' + QUOTENAME(@newIndex) + N' ON ' + QUOTENAME(@table)
                 + N' (TenantId, ' + QUOTENAME(@column) + N')'
                 + CASE WHEN @filter IS NOT NULL THEN N' WHERE ' + @filter ELSE N'' END;
        EXEC sp_executesql @sql;
    END

    FETCH NEXT FROM target_cursor INTO @table, @column;
END

CLOSE target_cursor;
DEALLOCATE target_cursor;
GO
