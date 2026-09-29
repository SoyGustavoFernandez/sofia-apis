-- Business-key uniques ignore soft-deleted rows (and NULL optional codes), missing uniques are added and money/quantity columns get the CHECKs the domain already enforces
DROP INDEX IF EXISTS UX_Aseguradoras_Medicas_Tenant_Codigo_Identificador_Nacional ON Aseguradoras_Medicas;
CREATE UNIQUE INDEX UX_Aseguradoras_Medicas_Tenant_Codigo_Identificador_Nacional ON Aseguradoras_Medicas (TenantId, Codigo_Identificador_Nacional) WHERE IsDeleted = 0;
GO

DROP INDEX IF EXISTS UX_DIGEMID_Actas_Destruccion_Tenant_Numero_Resolucion_Interna ON DIGEMID_Actas_Destruccion;
CREATE UNIQUE INDEX UX_DIGEMID_Actas_Destruccion_Tenant_Numero_Resolucion_Interna ON DIGEMID_Actas_Destruccion (TenantId, Numero_Resolucion_Interna) WHERE IsDeleted = 0;
GO

DROP INDEX IF EXISTS UX_DIGEMID_Catalogo_Productos_Tenant_Cod_Prod ON DIGEMID_Catalogo_Productos;
CREATE UNIQUE INDEX UX_DIGEMID_Catalogo_Productos_Tenant_Cod_Prod ON DIGEMID_Catalogo_Productos (TenantId, Cod_Prod) WHERE IsDeleted = 0;
GO

DROP INDEX IF EXISTS UX_Ingredientes_Activos_Tenant_Denominacion_DCI ON Ingredientes_Activos;
CREATE UNIQUE INDEX UX_Ingredientes_Activos_Tenant_Denominacion_DCI ON Ingredientes_Activos (TenantId, Denominacion_DCI) WHERE IsDeleted = 0;
GO

-- Blank optional codes are stored as NULL and never collide
UPDATE Laboratorios SET Codigo_Identificador = NULL WHERE LTRIM(RTRIM(Codigo_Identificador)) = '';
DROP INDEX IF EXISTS UX_Laboratorios_Tenant_Codigo_Identificador ON Laboratorios;
CREATE UNIQUE INDEX UX_Laboratorios_Tenant_Codigo_Identificador ON Laboratorios (TenantId, Codigo_Identificador) WHERE IsDeleted = 0 AND Codigo_Identificador IS NOT NULL;
GO

DROP INDEX IF EXISTS UX_Laboratorios_Tenant_Nombre_Compania ON Laboratorios;
CREATE UNIQUE INDEX UX_Laboratorios_Tenant_Nombre_Compania ON Laboratorios (TenantId, Nombre_Compania) WHERE IsDeleted = 0;
GO

DROP INDEX IF EXISTS UX_Medicamentos_Tenant_Codigo_Nacional ON Medicamentos;
CREATE UNIQUE INDEX UX_Medicamentos_Tenant_Codigo_Nacional ON Medicamentos (TenantId, Codigo_Nacional) WHERE IsDeleted = 0;
GO

DROP INDEX IF EXISTS UX_Pacientes_Clientes_Tenant_Doc_Identidad_Gub ON Pacientes_Clientes;
CREATE UNIQUE INDEX UX_Pacientes_Clientes_Tenant_Doc_Identidad_Gub ON Pacientes_Clientes (TenantId, Doc_Identidad_Gub) WHERE IsDeleted = 0;
GO

DROP INDEX IF EXISTS UX_Profesionales_Salud_Tenant_Numero_Registro ON Profesionales_Salud;
CREATE UNIQUE INDEX UX_Profesionales_Salud_Tenant_Numero_Registro ON Profesionales_Salud (TenantId, Numero_Registro) WHERE IsDeleted = 0;
GO

DROP INDEX IF EXISTS UX_Proveedores_Dist_Tenant_Tax_ID ON Proveedores_Dist;
CREATE UNIQUE INDEX UX_Proveedores_Dist_Tenant_Tax_ID ON Proveedores_Dist (TenantId, Tax_ID) WHERE IsDeleted = 0;
GO

DROP INDEX IF EXISTS UX_Proveedores_Dist_Tenant_Razon_Social ON Proveedores_Dist;
CREATE UNIQUE INDEX UX_Proveedores_Dist_Tenant_Razon_Social ON Proveedores_Dist (TenantId, Razon_Social) WHERE IsDeleted = 0;
GO

DROP INDEX IF EXISTS UX_Seguridad_Roles_Tenant_Nombre_Rol ON Seguridad_Roles;
CREATE UNIQUE INDEX UX_Seguridad_Roles_Tenant_Nombre_Rol ON Seguridad_Roles (TenantId, Nombre_Rol) WHERE IsDeleted = 0;
GO

DROP INDEX IF EXISTS UX_Unidades_Medida_Tenant_Codigo_UoM ON Unidades_Medida;
CREATE UNIQUE INDEX UX_Unidades_Medida_Tenant_Codigo_UoM ON Unidades_Medida (TenantId, Codigo_UoM) WHERE IsDeleted = 0 AND Codigo_UoM IS NOT NULL;
GO

-- Usernames stay global (login does not ask for the company) and one account per employee, both only among live accounts
DECLARE @sql NVARCHAR(MAX) = N'';
SELECT @sql = @sql + N'ALTER TABLE Seguridad_Cuentas DROP CONSTRAINT ' + QUOTENAME(i.name) + N'; '
FROM sys.indexes i
WHERE i.object_id = OBJECT_ID('Seguridad_Cuentas') AND i.is_unique_constraint = 1
  AND (SELECT COUNT(*) FROM sys.index_columns ic WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id) = 1
  AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
              WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id AND c.name IN ('Nombre_Usuario', 'Empleado_ID'));
EXEC sp_executesql @sql;
GO

DROP INDEX IF EXISTS UX_Seguridad_Cuentas_Nombre_Usuario ON Seguridad_Cuentas;
CREATE UNIQUE INDEX UX_Seguridad_Cuentas_Nombre_Usuario ON Seguridad_Cuentas (Nombre_Usuario) WHERE IsDeleted = 0;
GO

DROP INDEX IF EXISTS UX_Seguridad_Cuentas_Empleado_ID ON Seguridad_Cuentas;
CREATE UNIQUE INDEX UX_Seguridad_Cuentas_Empleado_ID ON Seguridad_Cuentas (Empleado_ID) WHERE IsDeleted = 0;
GO

-- The description is copied from the sale unit, so the unit is the real business key of a presentation
DROP INDEX IF EXISTS UX_PresentacionesVenta_Producto_Descripcion ON Presentaciones_Venta;
DROP INDEX IF EXISTS UX_Presentaciones_Venta_Producto_UnidadVenta ON Presentaciones_Venta;
CREATE UNIQUE INDEX UX_Presentaciones_Venta_Producto_UnidadVenta ON Presentaciones_Venta (Producto_ID, Unidad_Venta_ID) WHERE IsDeleted = 0;
GO

DROP INDEX IF EXISTS UX_Formulacion_Clinica_Producto_Ingrediente ON Formulacion_Clinica;
CREATE UNIQUE INDEX UX_Formulacion_Clinica_Producto_Ingrediente ON Formulacion_Clinica (Producto_ID, Ingrediente_ID) WHERE IsDeleted = 0;
GO

DROP INDEX IF EXISTS UX_Jerarquia_UoM_Producto_Mayor_Menor ON Jerarquia_UoM;
CREATE UNIQUE INDEX UX_Jerarquia_UoM_Producto_Mayor_Menor ON Jerarquia_UoM (Producto_ID, UoM_Mayor_ID, UoM_Menor_ID) WHERE IsDeleted = 0;
GO

ALTER TABLE Ventas_Cabecera DROP CONSTRAINT IF EXISTS CHK_Ventas_Cabecera_Monto_Total_Bruto;
ALTER TABLE Ventas_Cabecera ADD CONSTRAINT CHK_Ventas_Cabecera_Monto_Total_Bruto CHECK (Monto_Total_Bruto >= 0);
GO

ALTER TABLE Ventas_Pagos DROP CONSTRAINT IF EXISTS CHK_Ventas_Pagos_Monto_Pagado;
ALTER TABLE Ventas_Pagos ADD CONSTRAINT CHK_Ventas_Pagos_Monto_Pagado CHECK (Monto_Pagado > 0);
GO

ALTER TABLE Ventas_Detalle DROP CONSTRAINT IF EXISTS CHK_Ventas_Detalle_Cantidad_Vendida;
ALTER TABLE Ventas_Detalle ADD CONSTRAINT CHK_Ventas_Detalle_Cantidad_Vendida CHECK (Cantidad_Vendida > 0);
ALTER TABLE Ventas_Detalle DROP CONSTRAINT IF EXISTS CHK_Ventas_Detalle_Precio_Fijado_Unidad;
ALTER TABLE Ventas_Detalle ADD CONSTRAINT CHK_Ventas_Detalle_Precio_Fijado_Unidad CHECK (Precio_Fijado_Unidad >= 0);
GO

ALTER TABLE Transferencias_Det DROP CONSTRAINT IF EXISTS CHK_Transferencias_Det_Cantidad_Enviada;
ALTER TABLE Transferencias_Det ADD CONSTRAINT CHK_Transferencias_Det_Cantidad_Enviada CHECK (Cantidad_Enviada > 0);
ALTER TABLE Transferencias_Det DROP CONSTRAINT IF EXISTS CHK_Transferencias_Det_Cantidad_Recibida;
ALTER TABLE Transferencias_Det ADD CONSTRAINT CHK_Transferencias_Det_Cantidad_Recibida CHECK (Cantidad_Recibida >= 0 AND Cantidad_Recibida <= Cantidad_Enviada);
GO

ALTER TABLE Presentaciones_Venta DROP CONSTRAINT IF EXISTS CHK_Presentaciones_Venta_Cantidad_Unidades_Base;
ALTER TABLE Presentaciones_Venta ADD CONSTRAINT CHK_Presentaciones_Venta_Cantidad_Unidades_Base CHECK (Cantidad_Unidades_Base > 0);
ALTER TABLE Presentaciones_Venta DROP CONSTRAINT IF EXISTS CHK_Presentaciones_Venta_Precio_Venta;
ALTER TABLE Presentaciones_Venta ADD CONSTRAINT CHK_Presentaciones_Venta_Precio_Venta CHECK (Precio_Venta >= 0);
GO

ALTER TABLE Historial_Precios_Prov DROP CONSTRAINT IF EXISTS CHK_Historial_Precios_Prov_Costo;
ALTER TABLE Historial_Precios_Prov ADD CONSTRAINT CHK_Historial_Precios_Prov_Costo CHECK (Costo_Por_Unidad_Base >= 0);
ALTER TABLE Historial_Precios_Prov DROP CONSTRAINT IF EXISTS CHK_Historial_Precios_Prov_Lead_Time;
ALTER TABLE Historial_Precios_Prov ADD CONSTRAINT CHK_Historial_Precios_Prov_Lead_Time CHECK (Lead_Time_Dias >= 0);
ALTER TABLE Historial_Precios_Prov DROP CONSTRAINT IF EXISTS CHK_Historial_Precios_Prov_Cantidad_Min;
ALTER TABLE Historial_Precios_Prov ADD CONSTRAINT CHK_Historial_Precios_Prov_Cantidad_Min CHECK (Cantidad_Min_Compra >= 1);
GO

ALTER TABLE Jerarquia_UoM DROP CONSTRAINT IF EXISTS CHK_Jerarquia_UoM_Multiplicador;
ALTER TABLE Jerarquia_UoM ADD CONSTRAINT CHK_Jerarquia_UoM_Multiplicador CHECK (Multiplicador > 0);
GO

ALTER TABLE Devoluciones_Detalle DROP CONSTRAINT IF EXISTS CHK_Devoluciones_Detalle_Cantidad_Devuelta;
ALTER TABLE Devoluciones_Detalle ADD CONSTRAINT CHK_Devoluciones_Detalle_Cantidad_Devuelta CHECK (Cantidad_Devuelta > 0);
GO

ALTER TABLE Devoluciones_Cabecera DROP CONSTRAINT IF EXISTS CHK_Devoluciones_Cabecera_Monto_Reembolsado;
ALTER TABLE Devoluciones_Cabecera ADD CONSTRAINT CHK_Devoluciones_Cabecera_Monto_Reembolsado CHECK (Monto_Reembolsado >= 0);
GO

ALTER TABLE POS_Sesiones_Caja DROP CONSTRAINT IF EXISTS CHK_POS_Sesiones_Caja_Montos;
ALTER TABLE POS_Sesiones_Caja ADD CONSTRAINT CHK_POS_Sesiones_Caja_Montos CHECK (Monto_Apertura_Efectivo >= 0 AND Monto_Cierre_Calculado >= 0 AND Monto_Cierre_Declarado >= 0);
GO

ALTER TABLE Medicamentos DROP CONSTRAINT IF EXISTS CHK_Medicamentos_Precio_Venta_Base;
ALTER TABLE Medicamentos ADD CONSTRAINT CHK_Medicamentos_Precio_Venta_Base CHECK (Precio_Venta_Base >= 0);
GO
