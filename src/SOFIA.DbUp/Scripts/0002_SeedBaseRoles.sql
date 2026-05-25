-- ==============================================================================
-- 0002: SEED BASE ROLES FOR RBAC
-- ==============================================================================

IF NOT EXISTS (SELECT 1 FROM Seguridad_Roles WHERE Nombre_Rol = 'Admin')
BEGIN
    INSERT INTO Seguridad_Roles (Rol_ID, Nombre_Rol, Descripcion, Nivel_Jerarquia, CreatedAt, CreatedBy, IsDeleted)
    VALUES (NEWID(), 'Admin', 'Administrador del Sistema - Acceso Total', 100, SYSDATETIMEOFFSET(), 'System', 0);
END

IF NOT EXISTS (SELECT 1 FROM Seguridad_Roles WHERE Nombre_Rol = 'Gerente')
BEGIN
    INSERT INTO Seguridad_Roles (Rol_ID, Nombre_Rol, Descripcion, Nivel_Jerarquia, CreatedAt, CreatedBy, IsDeleted)
    VALUES (NEWID(), 'Gerente', 'Gerente de Sucursal - Gestión Operativa', 80, SYSDATETIMEOFFSET(), 'System', 0);
END

IF NOT EXISTS (SELECT 1 FROM Seguridad_Roles WHERE Nombre_Rol = 'Farmaceutico')
BEGIN
    INSERT INTO Seguridad_Roles (Rol_ID, Nombre_Rol, Descripcion, Nivel_Jerarquia, CreatedAt, CreatedBy, IsDeleted)
    VALUES (NEWID(), 'Farmaceutico', 'Químico Farmacéutico - Responsable Clínico y Regencia', 70, SYSDATETIMEOFFSET(), 'System', 0);
END

IF NOT EXISTS (SELECT 1 FROM Seguridad_Roles WHERE Nombre_Rol = 'Tecnico')
BEGIN
    INSERT INTO Seguridad_Roles (Rol_ID, Nombre_Rol, Descripcion, Nivel_Jerarquia, CreatedAt, CreatedBy, IsDeleted)
    VALUES (NEWID(), 'Tecnico', 'Técnico en Farmacia - Logística e Inventario', 50, SYSDATETIMEOFFSET(), 'System', 0);
END

IF NOT EXISTS (SELECT 1 FROM Seguridad_Roles WHERE Nombre_Rol = 'Cajero')
BEGIN
    INSERT INTO Seguridad_Roles (Rol_ID, Nombre_Rol, Descripcion, Nivel_Jerarquia, CreatedAt, CreatedBy, IsDeleted)
    VALUES (NEWID(), 'Cajero', 'Vendedor / Cajero - Atención al Público y POS', 30, SYSDATETIMEOFFSET(), 'System', 0);
END

IF NOT EXISTS (SELECT 1 FROM Seguridad_Roles WHERE Nombre_Rol = 'Auditor')
BEGIN
    INSERT INTO Seguridad_Roles (Rol_ID, Nombre_Rol, Descripcion, Nivel_Jerarquia, CreatedAt, CreatedBy, IsDeleted)
    VALUES (NEWID(), 'Auditor', 'Auditor Externo - Solo Lectura y Reportes', 10, SYSDATETIMEOFFSET(), 'System', 0);
END
