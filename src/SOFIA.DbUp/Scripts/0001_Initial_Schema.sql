-- ==============================================================================
-- 0001: INITIAL SCHEMA - SOFIA PHARMACEUTICAL MANAGEMENT SYSTEM
-- ==============================================================================

-- 1. TOPOLOGÍA DE SUCURSALES Y CONTROL DE ACCESO
-- ------------------------------------------------------------------------------
CREATE TABLE Sucursales (
    Sucursal_ID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Nombre VARCHAR(100) NOT NULL,
    Direccion_Fisica VARCHAR(255) NOT NULL,
    Numero_Licencia VARCHAR(50) NOT NULL,
    Gerente_ID UNIQUEIDENTIFIER NULL,
    CreatedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy VARCHAR(100) NULL,
    LastModifiedAt DATETIMEOFFSET NULL,
    LastModifiedBy VARCHAR(100) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedAt DATETIMEOFFSET NULL,
    DeletedBy VARCHAR(100) NULL
);

CREATE TABLE Empleados (
    Empleado_ID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Sucursal_Base_ID UNIQUEIDENTIFIER NOT NULL,
    Nombres VARCHAR(75) NOT NULL,
    Apellido_Paterno VARCHAR(75) NOT NULL,
    Apellido_Materno VARCHAR(75) NOT NULL,
    Licencia_Prof VARCHAR(50) NULL,
    Huella_Biometrica VARBINARY(MAX) NULL,
    CreatedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy VARCHAR(100) NULL,
    LastModifiedAt DATETIMEOFFSET NULL,
    LastModifiedBy VARCHAR(100) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedAt DATETIMEOFFSET NULL,
    DeletedBy VARCHAR(100) NULL,
    CONSTRAINT FK_Empleados_Sucursal FOREIGN KEY (Sucursal_Base_ID) REFERENCES Sucursales(Sucursal_ID)
);

ALTER TABLE Sucursales 
ADD CONSTRAINT FK_Sucursal_Gerente FOREIGN KEY (Gerente_ID) REFERENCES Empleados(Empleado_ID);


-- 2. CATÁLOGO FARMACÉUTICO Y JERARQUÍAS UoM
-- ------------------------------------------------------------------------------
CREATE TABLE Laboratorios (
    Laboratorio_ID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Nombre_Compania VARCHAR(150) NOT NULL UNIQUE,
    Codigo_Identificador VARCHAR(50) UNIQUE,
    CreatedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy VARCHAR(100) NULL,
    LastModifiedAt DATETIMEOFFSET NULL,
    LastModifiedBy VARCHAR(100) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedAt DATETIMEOFFSET NULL,
    DeletedBy VARCHAR(100) NULL
);

CREATE TABLE Unidades_Medida (
    UoM_ID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Codigo_UoM VARCHAR(10) UNIQUE,
    Descripcion VARCHAR(50) NOT NULL,
    CreatedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy VARCHAR(100) NULL,
    LastModifiedAt DATETIMEOFFSET NULL,
    LastModifiedBy VARCHAR(100) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedAt DATETIMEOFFSET NULL,
    DeletedBy VARCHAR(100) NULL
);

CREATE TABLE Medicamentos (
    Producto_ID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Codigo_Nacional VARCHAR(50) NOT NULL UNIQUE,
    Nombre_Comercial VARCHAR(150) NOT NULL,
    Laboratorio_ID UNIQUEIDENTIFIER NOT NULL,
    Unidad_Base_ID UNIQUEIDENTIFIER NOT NULL,
    Condicion_Venta VARCHAR(20) NOT NULL,
    CreatedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy VARCHAR(100) NULL,
    LastModifiedAt DATETIMEOFFSET NULL,
    LastModifiedBy VARCHAR(100) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedAt DATETIMEOFFSET NULL,
    DeletedBy VARCHAR(100) NULL,
    CONSTRAINT CHK_Condicion_Venta CHECK (Condicion_Venta IN ('Venta Libre (OTC)', 'Receta Simple', 'Receta Retenida', 'Estupefaciente')),
    CONSTRAINT FK_Medicamentos_Lab FOREIGN KEY (Laboratorio_ID) REFERENCES Laboratorios(Laboratorio_ID),
    CONSTRAINT FK_Medicamentos_UoM FOREIGN KEY (Unidad_Base_ID) REFERENCES Unidades_Medida(UoM_ID)
);

CREATE TABLE Jerarquia_UoM (
    Conversion_ID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Producto_ID UNIQUEIDENTIFIER NOT NULL,
    UoM_Mayor_ID UNIQUEIDENTIFIER NOT NULL,
    UoM_Menor_ID UNIQUEIDENTIFIER NOT NULL,
    Multiplicador DECIMAL(12,4) NOT NULL,
    CreatedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy VARCHAR(100) NULL,
    LastModifiedAt DATETIMEOFFSET NULL,
    LastModifiedBy VARCHAR(100) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedAt DATETIMEOFFSET NULL,
    DeletedBy VARCHAR(100) NULL,
    CONSTRAINT FK_Jerarquia_Prod FOREIGN KEY (Producto_ID) REFERENCES Medicamentos(Producto_ID),
    CONSTRAINT FK_Jerarquia_UoMMayor FOREIGN KEY (UoM_Mayor_ID) REFERENCES Unidades_Medida(UoM_ID),
    CONSTRAINT FK_Jerarquia_UoMMenor FOREIGN KEY (UoM_Menor_ID) REFERENCES Unidades_Medida(UoM_ID)
);


-- 3. MOTOR CLÍNICO DE SUSTITUCIÓN (ATC Y DCI)
-- ------------------------------------------------------------------------------
CREATE TABLE Ingredientes_Activos (
    Ingrediente_ID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Denominacion_DCI VARCHAR(255) NOT NULL UNIQUE,
    Codigo_ATC VARCHAR(15) NOT NULL,
    CreatedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy VARCHAR(100) NULL,
    LastModifiedAt DATETIMEOFFSET NULL,
    LastModifiedBy VARCHAR(100) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedAt DATETIMEOFFSET NULL,
    DeletedBy VARCHAR(100) NULL
);

CREATE NONCLUSTERED INDEX IX_Ingredientes_ATC ON Ingredientes_Activos(Codigo_ATC);

CREATE TABLE Formulacion_Clinica (
    Formulacion_ID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Producto_ID UNIQUEIDENTIFIER NOT NULL,
    Ingrediente_ID UNIQUEIDENTIFIER NOT NULL,
    Concentracion_Dosis DECIMAL(12,4) NOT NULL,
    Unidad_Dosis_Clinica VARCHAR(20) NOT NULL,
    Codigo_TE_Orange VARCHAR(5) NULL,
    CreatedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy VARCHAR(100) NULL,
    LastModifiedAt DATETIMEOFFSET NULL,
    LastModifiedBy VARCHAR(100) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedAt DATETIMEOFFSET NULL,
    DeletedBy VARCHAR(100) NULL,
    CONSTRAINT FK_Formulacion_Prod FOREIGN KEY (Producto_ID) REFERENCES Medicamentos(Producto_ID),
    CONSTRAINT FK_Formulacion_Ingr FOREIGN KEY (Ingrediente_ID) REFERENCES Ingredientes_Activos(Ingrediente_ID)
);


-- 4. INVENTARIOS, LOTES Y TRANSFERENCIAS
-- ------------------------------------------------------------------------------
CREATE TABLE Lotes_Inventario (
    Lote_ID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Producto_ID UNIQUEIDENTIFIER NOT NULL,
    Numero_Lote_Mfr VARCHAR(100) NOT NULL,
    Fecha_Fabricacion DATETIMEOFFSET NULL,
    Fecha_Caducidad DATETIMEOFFSET NOT NULL,
    CreatedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy VARCHAR(100) NULL,
    LastModifiedAt DATETIMEOFFSET NULL,
    LastModifiedBy VARCHAR(100) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedAt DATETIMEOFFSET NULL,
    DeletedBy VARCHAR(100) NULL,
    CONSTRAINT FK_Lotes_Prod FOREIGN KEY (Producto_ID) REFERENCES Medicamentos(Producto_ID)
);

CREATE UNIQUE NONCLUSTERED INDEX UX_Lotes_Producto_NumeroLote ON Lotes_Inventario(Producto_ID, Numero_Lote_Mfr) WHERE IsDeleted = 0;

CREATE NONCLUSTERED INDEX IX_Lotes_Caducidad ON Lotes_Inventario(Fecha_Caducidad);

CREATE TABLE Inventario_Sucursal (
    Registro_Inv_ID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Sucursal_ID UNIQUEIDENTIFIER NOT NULL,
    Lote_ID UNIQUEIDENTIFIER NOT NULL,
    Cantidad_Fisica DECIMAL(12,4) NOT NULL,
    CreatedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy VARCHAR(100) NULL,
    LastModifiedAt DATETIMEOFFSET NULL,
    LastModifiedBy VARCHAR(100) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedAt DATETIMEOFFSET NULL,
    DeletedBy VARCHAR(100) NULL,
    CONSTRAINT CHK_Inventario_Positivo CHECK (Cantidad_Fisica >= 0),
    CONSTRAINT FK_InvSuc_Sucursal FOREIGN KEY (Sucursal_ID) REFERENCES Sucursales(Sucursal_ID),
    CONSTRAINT FK_InvSuc_Lote FOREIGN KEY (Lote_ID) REFERENCES Lotes_Inventario(Lote_ID)
);

CREATE TABLE Transferencias_Cab (
    Transferencia_ID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Sucursal_Origen UNIQUEIDENTIFIER NOT NULL,
    Sucursal_Destino UNIQUEIDENTIFIER NOT NULL,
    Estado_Logistico VARCHAR(30) NOT NULL,
    Empleado_Emisor UNIQUEIDENTIFIER NOT NULL,
    Empleado_Receptor UNIQUEIDENTIFIER NULL,
    Fecha_Despacho DATETIME NOT NULL,
    Fecha_Recepcion DATETIME NULL,
    CreatedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy VARCHAR(100) NULL,
    LastModifiedAt DATETIMEOFFSET NULL,
    LastModifiedBy VARCHAR(100) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedAt DATETIMEOFFSET NULL,
    DeletedBy VARCHAR(100) NULL,
    CONSTRAINT CHK_Estado_Logistico CHECK (Estado_Logistico IN ('Iniciada', 'Aprobada', 'En_Transito', 'Recibida_Parcial', 'Completada', 'Cancelada')),
    CONSTRAINT FK_TransCab_Origen FOREIGN KEY (Sucursal_Origen) REFERENCES Sucursales(Sucursal_ID),
    CONSTRAINT FK_TransCab_Destino FOREIGN KEY (Sucursal_Destino) REFERENCES Sucursales(Sucursal_ID),
    CONSTRAINT FK_TransCab_Emisor FOREIGN KEY (Empleado_Emisor) REFERENCES Empleados(Empleado_ID),
    CONSTRAINT FK_TransCab_Receptor FOREIGN KEY (Empleado_Receptor) REFERENCES Empleados(Empleado_ID)
);

CREATE TABLE Transferencias_Det (
    Detalle_Transf_ID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Transferencia_ID UNIQUEIDENTIFIER NOT NULL,
    Lote_ID UNIQUEIDENTIFIER NOT NULL,
    Cantidad_Enviada DECIMAL(12,4) NOT NULL,
    Cantidad_Recibida DECIMAL(12,4) NULL,
    CreatedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy VARCHAR(100) NULL,
    LastModifiedAt DATETIMEOFFSET NULL,
    LastModifiedBy VARCHAR(100) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedAt DATETIMEOFFSET NULL,
    DeletedBy VARCHAR(100) NULL,
    CONSTRAINT FK_TransDet_Cab FOREIGN KEY (Transferencia_ID) REFERENCES Transferencias_Cab(Transferencia_ID),
    CONSTRAINT FK_TransDet_Lote FOREIGN KEY (Lote_ID) REFERENCES Lotes_Inventario(Lote_ID)
);


-- 5. ADQUISICIONES Y PROVEEDORES
-- ------------------------------------------------------------------------------
CREATE TABLE Proveedores_Dist (
    Proveedor_ID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Razon_Social VARCHAR(200) NOT NULL UNIQUE,
    Tax_ID VARCHAR(50) NOT NULL UNIQUE,
    Terminos_Financieros VARCHAR(100) NULL,
    Calificacion_ESG DECIMAL(5,2) NULL,
    Tasa_Cumplimiento DECIMAL(5,2) DEFAULT 100.00,
    CreatedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy VARCHAR(100) NULL,
    LastModifiedAt DATETIMEOFFSET NULL,
    LastModifiedBy VARCHAR(100) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedAt DATETIMEOFFSET NULL,
    DeletedBy VARCHAR(100) NULL
);

CREATE TABLE Historial_Precios_Prov (
    Cotizacion_ID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Proveedor_ID UNIQUEIDENTIFIER NOT NULL,
    Producto_ID UNIQUEIDENTIFIER NOT NULL,
    Costo_Por_Unidad_Base DECIMAL(12,4) NOT NULL,
    Fecha_Inicio_Vigencia DATETIME NOT NULL,
    Fecha_Fin_Vigencia DATETIME NULL,
    Lead_Time_Dias INT NOT NULL,
    Cantidad_Min_Compra INT DEFAULT 1,
    CreatedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy VARCHAR(100) NULL,
    LastModifiedAt DATETIMEOFFSET NULL,
    LastModifiedBy VARCHAR(100) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedAt DATETIMEOFFSET NULL,
    DeletedBy VARCHAR(100) NULL,
    CONSTRAINT FK_HistPrecio_Prov FOREIGN KEY (Proveedor_ID) REFERENCES Proveedores_Dist(Proveedor_ID),
    CONSTRAINT FK_HistPrecio_Prod FOREIGN KEY (Producto_ID) REFERENCES Medicamentos(Producto_ID)
);


-- 6. POS, PERFILES Y RECETAS MÉDICAS (EHR Ligero)
-- ------------------------------------------------------------------------------
CREATE TABLE Pacientes_Clientes (
    Cliente_ID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Doc_Identidad_Gub VARCHAR(50) NOT NULL UNIQUE,
    Nombre_Apellidos VARCHAR(200) NOT NULL,
    Fecha_Nacimiento DATE NOT NULL,
    Contacto_Primario VARCHAR(100) NULL,
    CreatedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy VARCHAR(100) NULL,
    LastModifiedAt DATETIMEOFFSET NULL,
    LastModifiedBy VARCHAR(100) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedAt DATETIMEOFFSET NULL,
    DeletedBy VARCHAR(100) NULL
);

CREATE TABLE Profesionales_Salud (
    Medico_ID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Numero_Registro VARCHAR(50) NOT NULL UNIQUE,
    Nombre_Prescriptor VARCHAR(150) NOT NULL,
    Direccion_Clinica VARCHAR(255) NULL,
    CreatedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy VARCHAR(100) NULL,
    LastModifiedAt DATETIMEOFFSET NULL,
    LastModifiedBy VARCHAR(100) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedAt DATETIMEOFFSET NULL,
    DeletedBy VARCHAR(100) NULL
);

CREATE TABLE Recetas_Medicas (
    Receta_ID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Cliente_ID UNIQUEIDENTIFIER NOT NULL,
    Medico_ID UNIQUEIDENTIFIER NOT NULL,
    Fecha_Expedicion DATE NOT NULL,
    Repeticiones_Max INT DEFAULT 0,
    Indicaciones_Uso VARCHAR(MAX) NULL,
    CreatedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy VARCHAR(100) NULL,
    LastModifiedAt DATETIMEOFFSET NULL,
    LastModifiedBy VARCHAR(100) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedAt DATETIMEOFFSET NULL,
    DeletedBy VARCHAR(100) NULL,
    CONSTRAINT FK_Receta_Cliente FOREIGN KEY (Cliente_ID) REFERENCES Pacientes_Clientes(Cliente_ID),
    CONSTRAINT FK_Receta_Medico FOREIGN KEY (Medico_ID) REFERENCES Profesionales_Salud(Medico_ID)
);

CREATE TABLE POS_Sesiones_Caja (
    Sesion_ID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Sucursal_ID UNIQUEIDENTIFIER NOT NULL,
    Empleado_ID UNIQUEIDENTIFIER NOT NULL,
    Fecha_Hora_Apertura DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    Fecha_Hora_Cierre DATETIME NULL,
    Monto_Apertura_Efectivo DECIMAL(10,2) NOT NULL,
    Monto_Cierre_Calculado DECIMAL(10,2) NULL,
    Monto_Cierre_Declarado DECIMAL(10,2) NULL,
    Diferencia_Arqueo DECIMAL(10,2) NULL,
    Estado_Sesion VARCHAR(20) NOT NULL DEFAULT 'Abierta',
    CreatedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy VARCHAR(100) NULL,
    LastModifiedAt DATETIMEOFFSET NULL,
    LastModifiedBy VARCHAR(100) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedAt DATETIMEOFFSET NULL,
    DeletedBy VARCHAR(100) NULL,
    CONSTRAINT CHK_Estado_Sesion CHECK (Estado_Sesion IN ('Abierta', 'Cerrada', 'Cuadrada')),
    CONSTRAINT FK_POSSesion_Sucursal FOREIGN KEY (Sucursal_ID) REFERENCES Sucursales(Sucursal_ID),
    CONSTRAINT FK_POSSesion_Empleado FOREIGN KEY (Empleado_ID) REFERENCES Empleados(Empleado_ID)
);

CREATE TABLE Ventas_Cabecera (
    Transaccion_ID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Sucursal_ID UNIQUEIDENTIFIER NOT NULL,
    Empleado_ID UNIQUEIDENTIFIER NOT NULL,
    Cliente_ID UNIQUEIDENTIFIER NULL,
    Sesion_ID UNIQUEIDENTIFIER NULL,
    Fecha_Hora_UTC DATETIME DEFAULT CURRENT_TIMESTAMP,
    Monto_Total_Bruto DECIMAL(12,2) NOT NULL,
    Estado VARCHAR(20) NOT NULL DEFAULT 'Completada',
    Motivo_Anulacion VARCHAR(255) NULL,
    CreatedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy VARCHAR(100) NULL,
    LastModifiedAt DATETIMEOFFSET NULL,
    LastModifiedBy VARCHAR(100) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedAt DATETIMEOFFSET NULL,
    DeletedBy VARCHAR(100) NULL,
    CONSTRAINT FK_VentaCab_Sucursal FOREIGN KEY (Sucursal_ID) REFERENCES Sucursales(Sucursal_ID),
    CONSTRAINT FK_VentaCab_Empleado FOREIGN KEY (Empleado_ID) REFERENCES Empleados(Empleado_ID),
    CONSTRAINT FK_VentaCab_Sesion FOREIGN KEY (Sesion_ID) REFERENCES POS_Sesiones_Caja(Sesion_ID),
    CONSTRAINT FK_VentaCab_Cliente FOREIGN KEY (Cliente_ID) REFERENCES Pacientes_Clientes(Cliente_ID),
    CONSTRAINT CHK_Venta_Estado CHECK (Estado IN ('Completada', 'Anulada', 'Devuelta', 'Pendiente'))
);

CREATE TABLE Ventas_Detalle (
    Detalle_ID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Transaccion_ID UNIQUEIDENTIFIER NOT NULL,
    Lote_ID UNIQUEIDENTIFIER NOT NULL,
    Receta_ID UNIQUEIDENTIFIER NULL,
    Cantidad_Vendida DECIMAL(8,2) NOT NULL,
    Precio_Fijado_Unidad DECIMAL(10,2) NOT NULL,
    Costo_Unitario_Historico DECIMAL(10,2) NOT NULL,
    CreatedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy VARCHAR(100) NULL,
    LastModifiedAt DATETIMEOFFSET NULL,
    LastModifiedBy VARCHAR(100) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedAt DATETIMEOFFSET NULL,
    DeletedBy VARCHAR(100) NULL,
    CONSTRAINT FK_VentaDet_Transaccion FOREIGN KEY (Transaccion_ID) REFERENCES Ventas_Cabecera(Transaccion_ID),
    CONSTRAINT FK_VentaDet_Lote FOREIGN KEY (Lote_ID) REFERENCES Lotes_Inventario(Lote_ID),
    CONSTRAINT FK_VentaDet_Receta FOREIGN KEY (Receta_ID) REFERENCES Recetas_Medicas(Receta_ID)
);


-- 7. SERVICIOS CLÍNICOS E INMUNIZACIÓN
-- ------------------------------------------------------------------------------
CREATE TABLE Servicios_Clinicos_Inmunizacion (
    Evento_Clinico_ID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Transaccion_ID UNIQUEIDENTIFIER NULL,
    Cliente_ID UNIQUEIDENTIFIER NOT NULL,
    Profesional_Admn_ID UNIQUEIDENTIFIER NOT NULL,
    Producto_ID UNIQUEIDENTIFIER NOT NULL,
    Lote_ID UNIQUEIDENTIFIER NOT NULL,
    Via_Administracion VARCHAR(50) NOT NULL,
    Sitio_Anatomico VARCHAR(100) NOT NULL,
    Volumen_Dosis DECIMAL(8,2) NOT NULL,
    Fecha_Admn_Fisica DATETIME DEFAULT CURRENT_TIMESTAMP,
    Fecha_Entrega_VIS DATE NULL,
    Modalidad_Registro VARCHAR(20) NOT NULL,
    CreatedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy VARCHAR(100) NULL,
    LastModifiedAt DATETIMEOFFSET NULL,
    LastModifiedBy VARCHAR(100) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedAt DATETIMEOFFSET NULL,
    DeletedBy VARCHAR(100) NULL,
    CONSTRAINT CHK_Modalidad_Reg CHECK (Modalidad_Registro IN ('Corriente', 'Historica')),
    CONSTRAINT FK_Inmunizacion_Trans FOREIGN KEY (Transaccion_ID) REFERENCES Ventas_Cabecera(Transaccion_ID),
    CONSTRAINT FK_Inmunizacion_Cliente FOREIGN KEY (Cliente_ID) REFERENCES Pacientes_Clientes(Cliente_ID),
    CONSTRAINT FK_Inmunizacion_Prof FOREIGN KEY (Profesional_Admn_ID) REFERENCES Empleados(Empleado_ID),
    CONSTRAINT FK_Inmunizacion_Prod FOREIGN KEY (Producto_ID) REFERENCES Medicamentos(Producto_ID),
    CONSTRAINT FK_Inmunizacion_Lote FOREIGN KEY (Lote_ID) REFERENCES Lotes_Inventario(Lote_ID)
);

CREATE TABLE Recetas_Digitalizadas_IA (
    Procesamiento_ID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Receta_ID UNIQUEIDENTIFIER NOT NULL,
    Ruta_Imagen_Blob VARCHAR(500) NOT NULL,
    Texto_Crudo_OCR VARCHAR(MAX) NULL,
    Entidades_Clinicas_Extraidas NVARCHAR(MAX) NULL,
    Nivel_Confianza_IA DECIMAL(5,2) NOT NULL,
    Requiere_Revision_Humana BIT DEFAULT 1,
    Fecha_Procesamiento DATETIME DEFAULT CURRENT_TIMESTAMP,
    CreatedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy VARCHAR(100) NULL,
    LastModifiedAt DATETIMEOFFSET NULL,
    LastModifiedBy VARCHAR(100) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedAt DATETIMEOFFSET NULL,
    DeletedBy VARCHAR(100) NULL,
    CONSTRAINT FK_RecetaIA_Receta FOREIGN KEY (Receta_ID) REFERENCES Recetas_Medicas(Receta_ID)
);

CREATE TABLE Aseguradoras_Medicas (
    Aseguradora_ID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Nombre_Comercial VARCHAR(150) NOT NULL,
    Codigo_Identificador_Nacional VARCHAR(50) UNIQUE NOT NULL,
    CreatedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy VARCHAR(100) NULL,
    LastModifiedAt DATETIMEOFFSET NULL,
    LastModifiedBy VARCHAR(100) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedAt DATETIMEOFFSET NULL,
    DeletedBy VARCHAR(100) NULL
);

CREATE TABLE Ventas_Reclamos_Seguro (
    Reclamo_ID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Detalle_ID UNIQUEIDENTIFIER NOT NULL,
    Aseguradora_ID UNIQUEIDENTIFIER NOT NULL,
    Monto_Cubierto DECIMAL(10,2) NOT NULL,
    Monto_Copago_Paciente DECIMAL(10,2) NOT NULL,
    Estado_Reclamo VARCHAR(50) NOT NULL,
    Codigo_Autorizacion VARCHAR(100) NULL,
    CreatedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy VARCHAR(100) NULL,
    LastModifiedAt DATETIMEOFFSET NULL,
    LastModifiedBy VARCHAR(100) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedAt DATETIMEOFFSET NULL,
    DeletedBy VARCHAR(100) NULL,
    CONSTRAINT FK_Reclamo_VentaDet FOREIGN KEY (Detalle_ID) REFERENCES Ventas_Detalle(Detalle_ID),
    CONSTRAINT FK_Reclamo_Aseguradora FOREIGN KEY (Aseguradora_ID) REFERENCES Aseguradoras_Medicas(Aseguradora_ID)
);

CREATE TABLE Despachos_Delivery (
    Despacho_ID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Transaccion_ID UNIQUEIDENTIFIER NOT NULL,
    Plataforma_Servicio VARCHAR(50) NOT NULL,
    Codigo_Rastreo VARCHAR(100) NULL,
    Estado_Despacho VARCHAR(50) NOT NULL,
    Direccion_Entrega VARCHAR(255) NOT NULL,
    Repartidor_Nombre VARCHAR(150) NULL,
    Evidencia_Fotografica_URL VARCHAR(500) NULL,
    CreatedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy VARCHAR(100) NULL,
    LastModifiedAt DATETIMEOFFSET NULL,
    LastModifiedBy VARCHAR(100) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedAt DATETIMEOFFSET NULL,
    DeletedBy VARCHAR(100) NULL,
    CONSTRAINT CHK_Estado_Despacho CHECK (Estado_Despacho IN ('Preparando', 'En_Camino', 'Entregado', 'Devuelto')),
    CONSTRAINT FK_Delivery_VentaCab FOREIGN KEY (Transaccion_ID) REFERENCES Ventas_Cabecera(Transaccion_ID)
);

CREATE TABLE Auditoria_Eventos_Seguridad (
    Evento_ID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Empleado_ID UNIQUEIDENTIFIER NOT NULL,
    Tabla_Afectada VARCHAR(50) NOT NULL,
    Registro_ID_Afectado UNIQUEIDENTIFIER NOT NULL,
    Tipo_Accion VARCHAR(20) NOT NULL,
    Payload_Anterior NVARCHAR(MAX) NULL,
    Payload_Nuevo NVARCHAR(MAX) NULL,
    Fecha_Hora_Evento DATETIME DEFAULT CURRENT_TIMESTAMP,
    Direccion_IP VARCHAR(45) NULL,
    CreatedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy VARCHAR(100) NULL,
    LastModifiedAt DATETIMEOFFSET NULL,
    LastModifiedBy VARCHAR(100) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedAt DATETIMEOFFSET NULL,
    DeletedBy VARCHAR(100) NULL,
    CONSTRAINT FK_Auditoria_Empleado FOREIGN KEY (Empleado_ID) REFERENCES Empleados(Empleado_ID)
);

CREATE TABLE Sistema_Outbox_Eventos (
    Evento_Outbox_ID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Tipo_Evento VARCHAR(100) NOT NULL,
    Payload_JSON NVARCHAR(MAX) NOT NULL,
    Fecha_Creacion DATETIME DEFAULT CURRENT_TIMESTAMP,
    Procesado BIT DEFAULT 0,
    Fecha_Procesamiento DATETIME NULL,
    Error_Publicacion VARCHAR(MAX) NULL,
    CreatedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy VARCHAR(100) NULL,
    LastModifiedAt DATETIMEOFFSET NULL,
    LastModifiedBy VARCHAR(100) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedAt DATETIMEOFFSET NULL,
    DeletedBy VARCHAR(100) NULL
);

CREATE TABLE Registro_Privacidad_Presidio (
    Anonimizacion_ID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Procesamiento_ID UNIQUEIDENTIFIER NOT NULL,
    Entidad_Detectada VARCHAR(50) NOT NULL,
    Texto_Original_Encriptado VARBINARY(MAX) NOT NULL,
    Texto_Reemplazo VARCHAR(50) NOT NULL,
    Nivel_Riesgo_PII DECIMAL(5,2) NULL,
    Fecha_Auditoria DATETIME DEFAULT CURRENT_TIMESTAMP,
    CreatedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy VARCHAR(100) NULL,
    LastModifiedAt DATETIMEOFFSET NULL,
    LastModifiedBy VARCHAR(100) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedAt DATETIMEOFFSET NULL,
    DeletedBy VARCHAR(100) NULL,
    CONSTRAINT FK_Privacidad_Procesamiento FOREIGN KEY (Procesamiento_ID) REFERENCES Recetas_Digitalizadas_IA(Procesamiento_ID)
);


-- 8. FACTURACIÓN ELECTRÓNICA (Integración SUNAT / OSE)
-- ------------------------------------------------------------------------------
CREATE TABLE SUNAT_Series_Fiscales (
    Serie_ID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Sucursal_ID UNIQUEIDENTIFIER NOT NULL,
    Tipo_Comprobante VARCHAR(2) NOT NULL,
    Prefijo_Serie VARCHAR(4) NOT NULL,
    Correlativo_Actual INT NOT NULL DEFAULT 0,
    Estado_Serie VARCHAR(10) DEFAULT 'Activa',
    CreatedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy VARCHAR(100) NULL,
    LastModifiedAt DATETIMEOFFSET NULL,
    LastModifiedBy VARCHAR(100) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedAt DATETIMEOFFSET NULL,
    DeletedBy VARCHAR(100) NULL,
    CONSTRAINT CHK_Tipo_Comprobante CHECK (Tipo_Comprobante IN ('00', '01', '03', '07', '08')),
    CONSTRAINT FK_Series_Sucursal FOREIGN KEY (Sucursal_ID) REFERENCES Sucursales(Sucursal_ID)
);

CREATE TABLE SUNAT_Comprobantes_Emitidos (
    Comprobante_ID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Transaccion_ID UNIQUEIDENTIFIER NOT NULL,
    Serie_ID UNIQUEIDENTIFIER NOT NULL,
    Numero_Correlativo INT NOT NULL,
    Fecha_Emision DATETIME DEFAULT CURRENT_TIMESTAMP,
    Tipo_Doc_Identidad_Cliente VARCHAR(1) NOT NULL,
    Numero_Identidad_Cliente VARCHAR(20) NOT NULL,
    Razon_Social_Cliente VARCHAR(200) NOT NULL,
    Monto_Gravado_IGV DECIMAL(12,2) NOT NULL DEFAULT 0,
    Monto_Exonerado DECIMAL(12,2) NOT NULL DEFAULT 0,
    Monto_Total_IGV DECIMAL(12,2) NOT NULL DEFAULT 0,
    Monto_Total_Venta DECIMAL(12,2) NOT NULL,
    Hash_Firma_Digital VARCHAR(255) NULL,
    Estado_Aceptacion VARCHAR(20) DEFAULT 'Pendiente',
    Ruta_Archivo_XML VARCHAR(500) NULL,
    Ruta_Archivo_CDR VARCHAR(500) NULL,
    URL_Publica_Verificacion VARCHAR(500) NULL,
    CreatedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy VARCHAR(100) NULL,
    LastModifiedAt DATETIMEOFFSET NULL,
    LastModifiedBy VARCHAR(100) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedAt DATETIMEOFFSET NULL,
    DeletedBy VARCHAR(100) NULL,
    CONSTRAINT FK_Comprobante_Venta FOREIGN KEY (Transaccion_ID) REFERENCES Ventas_Cabecera(Transaccion_ID),
    CONSTRAINT FK_Comprobante_Serie FOREIGN KEY (Serie_ID) REFERENCES SUNAT_Series_Fiscales(Serie_ID)
);


-- 9. DEVOLUCIONES Y NOTAS DE CRÉDITO
-- ------------------------------------------------------------------------------
CREATE TABLE Devoluciones_Cabecera (
    Devolucion_ID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Comprobante_Origen_ID UNIQUEIDENTIFIER NOT NULL,
    Comprobante_NC_ID UNIQUEIDENTIFIER NULL,
    Empleado_Autoriza_ID UNIQUEIDENTIFIER NOT NULL,
    Motivo_SUNAT_Catalogo VARCHAR(2) NOT NULL,
    Sustento_Descriptivo VARCHAR(255) NOT NULL,
    Fecha_Devolucion DATETIME DEFAULT CURRENT_TIMESTAMP,
    CreatedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy VARCHAR(100) NULL,
    LastModifiedAt DATETIMEOFFSET NULL,
    LastModifiedBy VARCHAR(100) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedAt DATETIMEOFFSET NULL,
    DeletedBy VARCHAR(100) NULL,
    CONSTRAINT FK_Devolucion_Origen FOREIGN KEY (Comprobante_Origen_ID) REFERENCES SUNAT_Comprobantes_Emitidos(Comprobante_ID),
    CONSTRAINT FK_Devolucion_NC FOREIGN KEY (Comprobante_NC_ID) REFERENCES SUNAT_Comprobantes_Emitidos(Comprobante_ID),
    CONSTRAINT FK_Devolucion_Empleado FOREIGN KEY (Empleado_Autoriza_ID) REFERENCES Empleados(Empleado_ID)
);

CREATE TABLE Devoluciones_Detalle (
    Detalle_Dev_ID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Devolucion_ID UNIQUEIDENTIFIER NOT NULL,
    Detalle_Venta_ID UNIQUEIDENTIFIER NOT NULL,
    Cantidad_Devuelta DECIMAL(8,2) NOT NULL,
    Destino_Fisico_Logico VARCHAR(30) NOT NULL,
    CreatedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy VARCHAR(100) NULL,
    LastModifiedAt DATETIMEOFFSET NULL,
    LastModifiedBy VARCHAR(100) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedAt DATETIMEOFFSET NULL,
    DeletedBy VARCHAR(100) NULL,
    CONSTRAINT CHK_Destino_Devolucion CHECK (Destino_Fisico_Logico IN ('Reingreso_Venta', 'Cuarentena_DIGEMID')),
    CONSTRAINT FK_DevDet_Cabecera FOREIGN KEY (Devolucion_ID) REFERENCES Devoluciones_Cabecera(Devolucion_ID),
    CONSTRAINT FK_DevDet_VentaDet FOREIGN KEY (Detalle_Venta_ID) REFERENCES Ventas_Detalle(Detalle_ID)
);


-- 10. MERMAS, CUARENTENA Y DESTRUCCIÓN (Cumplimiento DIGEMID)
-- ------------------------------------------------------------------------------
CREATE TABLE DIGEMID_Inventario_Cuarentena (
    Registro_Cuarentena_ID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Sucursal_ID UNIQUEIDENTIFIER NOT NULL,
    Lote_ID UNIQUEIDENTIFIER NOT NULL,
    Detalle_Dev_ID UNIQUEIDENTIFIER NULL,
    Cantidad_Aislada DECIMAL(12,4) NOT NULL,
    Motivo_Aislamiento VARCHAR(50) NOT NULL,
    Fecha_Ingreso_Cuarentena DATETIME DEFAULT CURRENT_TIMESTAMP,
    Estado_Resolucion VARCHAR(20) DEFAULT 'Retenido',
    Empleado_Registra_ID UNIQUEIDENTIFIER NOT NULL,
    CreatedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy VARCHAR(100) NULL,
    LastModifiedAt DATETIMEOFFSET NULL,
    LastModifiedBy VARCHAR(100) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedAt DATETIMEOFFSET NULL,
    DeletedBy VARCHAR(100) NULL,
    CONSTRAINT FK_Cuarentena_Sucursal FOREIGN KEY (Sucursal_ID) REFERENCES Sucursales(Sucursal_ID),
    CONSTRAINT FK_Cuarentena_Lote FOREIGN KEY (Lote_ID) REFERENCES Lotes_Inventario(Lote_ID),
    CONSTRAINT FK_Cuarentena_Empleado FOREIGN KEY (Empleado_Registra_ID) REFERENCES Empleados(Empleado_ID),
    CONSTRAINT FK_Cuarentena_Devolucion FOREIGN KEY (Detalle_Dev_ID) REFERENCES Devoluciones_Detalle(Detalle_Dev_ID)
);

CREATE TABLE DIGEMID_Actas_Destruccion (
    Acta_ID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Numero_Resolucion_Interna VARCHAR(50) NOT NULL UNIQUE,
    Empresa_Residuos_Biocontaminados VARCHAR(150) NOT NULL,
    Manifiesto_Transporte_Doc VARCHAR(50) NULL,
    Fecha_Ejecucion DATE NOT NULL,
    Regente_Responsable_ID UNIQUEIDENTIFIER NOT NULL,
    Ruta_Acta_Firmada_PDF VARCHAR(500) NULL,
    CreatedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy VARCHAR(100) NULL,
    LastModifiedAt DATETIMEOFFSET NULL,
    LastModifiedBy VARCHAR(100) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedAt DATETIMEOFFSET NULL,
    DeletedBy VARCHAR(100) NULL,
    CONSTRAINT FK_Acta_Regente FOREIGN KEY (Regente_Responsable_ID) REFERENCES Empleados(Empleado_ID)
);

CREATE TABLE DIGEMID_Actas_Detalle (
    Detalle_Acta_ID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Acta_ID UNIQUEIDENTIFIER NOT NULL,
    Registro_Cuarentena_ID UNIQUEIDENTIFIER NOT NULL,
    Cantidad_Destruida DECIMAL(12,4) NOT NULL,
    CreatedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy VARCHAR(100) NULL,
    LastModifiedAt DATETIMEOFFSET NULL,
    LastModifiedBy VARCHAR(100) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedAt DATETIMEOFFSET NULL,
    DeletedBy VARCHAR(100) NULL,
    CONSTRAINT FK_ActaDet_Acta FOREIGN KEY (Acta_ID) REFERENCES DIGEMID_Actas_Destruccion(Acta_ID),
    CONSTRAINT FK_ActaDet_Cuarentena FOREIGN KEY (Registro_Cuarentena_ID) REFERENCES DIGEMID_Inventario_Cuarentena(Registro_Cuarentena_ID)
);


-- 11. FÓRMULAS MAGISTRALES
-- ------------------------------------------------------------------------------
CREATE TABLE Magistrales_Ordenes_Produccion (
    Orden_Produccion_ID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Sucursal_ID UNIQUEIDENTIFIER NOT NULL,
    Receta_ID UNIQUEIDENTIFIER NULL,
    Producto_Resultante_ID UNIQUEIDENTIFIER NOT NULL,
    Lote_Generado_ID UNIQUEIDENTIFIER NULL,
    Cantidad_Producida DECIMAL(12,4) NULL,
    Quimico_Preparador_ID UNIQUEIDENTIFIER NOT NULL,
    Fecha_Preparacion DATETIME DEFAULT CURRENT_TIMESTAMP,
    Estado_Produccion VARCHAR(20) DEFAULT 'En_Proceso',
    CreatedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy VARCHAR(100) NULL,
    LastModifiedAt DATETIMEOFFSET NULL,
    LastModifiedBy VARCHAR(100) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedAt DATETIMEOFFSET NULL,
    DeletedBy VARCHAR(100) NULL,
    CONSTRAINT FK_Magistral_Sucursal FOREIGN KEY (Sucursal_ID) REFERENCES Sucursales(Sucursal_ID),
    CONSTRAINT FK_Magistral_Receta FOREIGN KEY (Receta_ID) REFERENCES Recetas_Medicas(Receta_ID),
    CONSTRAINT FK_Magistral_Producto FOREIGN KEY (Producto_Resultante_ID) REFERENCES Medicamentos(Producto_ID),
    CONSTRAINT FK_Magistral_Lote FOREIGN KEY (Lote_Generado_ID) REFERENCES Lotes_Inventario(Lote_ID),
    CONSTRAINT FK_Magistral_Quimico FOREIGN KEY (Quimico_Preparador_ID) REFERENCES Empleados(Empleado_ID)
);

CREATE TABLE Magistrales_Consumo_Insumos (
    Consumo_ID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Orden_Produccion_ID UNIQUEIDENTIFIER NOT NULL,
    Lote_Materia_Prima_ID UNIQUEIDENTIFIER NOT NULL,
    Cantidad_Consumida DECIMAL(12,4) NOT NULL,
    Unidad_Medida VARCHAR(20) NOT NULL,
    CreatedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy VARCHAR(100) NULL,
    LastModifiedAt DATETIMEOFFSET NULL,
    LastModifiedBy VARCHAR(100) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedAt DATETIMEOFFSET NULL,
    DeletedBy VARCHAR(100) NULL,
    CONSTRAINT FK_Consumo_Orden FOREIGN KEY (Orden_Produccion_ID) REFERENCES Magistrales_Ordenes_Produccion(Orden_Produccion_ID),
    CONSTRAINT FK_Consumo_LoteMateria FOREIGN KEY (Lote_Materia_Prima_ID) REFERENCES Lotes_Inventario(Lote_ID)
);


-- 12. MÉTODOS DE PAGO Y AGENDA
-- ------------------------------------------------------------------------------
CREATE TABLE Ventas_Pagos (
    Pago_ID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Transaccion_ID UNIQUEIDENTIFIER NOT NULL,
    Metodo_Pago VARCHAR(50) NOT NULL,
    Monto_Pagado DECIMAL(12,2) NOT NULL,
    Referencia_Operacion VARCHAR(100) NULL,
    CreatedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy VARCHAR(100) NULL,
    LastModifiedAt DATETIMEOFFSET NULL,
    LastModifiedBy VARCHAR(100) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedAt DATETIMEOFFSET NULL,
    DeletedBy VARCHAR(100) NULL,
    Fecha_Pago DATETIME DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT FK_VentaPago_Transaccion FOREIGN KEY (Transaccion_ID) REFERENCES Ventas_Cabecera(Transaccion_ID)
);

CREATE TABLE Servicios_Agenda (
    Agenda_ID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Cliente_ID UNIQUEIDENTIFIER NOT NULL,
    Producto_ID UNIQUEIDENTIFIER NOT NULL,
    Venta_ID UNIQUEIDENTIFIER NULL,
    Fecha_Hora_Programada DATETIME NOT NULL,
    Estado_Cita VARCHAR(20) DEFAULT 'Programada',
    CreatedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy VARCHAR(100) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_Agenda_Cliente FOREIGN KEY (Cliente_ID) REFERENCES Pacientes_Clientes(Cliente_ID),
    CONSTRAINT FK_Agenda_Producto FOREIGN KEY (Producto_ID) REFERENCES Medicamentos(Producto_ID),
    CONSTRAINT FK_Agenda_Venta FOREIGN KEY (Venta_ID) REFERENCES Ventas_Cabecera(Transaccion_ID)
);

CREATE NONCLUSTERED INDEX IX_Agenda_Fecha ON Servicios_Agenda(Fecha_Hora_Programada) WHERE IsDeleted = 0;

CREATE TABLE Sistema_Notificaciones_Internas (
    Notificacion_ID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Empleado_Origen_ID UNIQUEIDENTIFIER NOT NULL,
    Empleado_Destino_ID UNIQUEIDENTIFIER NULL,
    Sucursal_Destino_ID UNIQUEIDENTIFIER NOT NULL,
    Mensaje_Texto NVARCHAR(500) NOT NULL,
    Entidad_Relacionada VARCHAR(50) NULL,
    Entidad_ID UNIQUEIDENTIFIER NULL,
    Leido BIT DEFAULT 0,
    Fecha_Lectura DATETIME NULL,
    CreatedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_Notif_Origen FOREIGN KEY (Empleado_Origen_ID) REFERENCES Empleados(Empleado_ID),
    CONSTRAINT FK_Notif_Destino FOREIGN KEY (Empleado_Destino_ID) REFERENCES Empleados(Empleado_ID),
    CONSTRAINT FK_Notif_Sucursal FOREIGN KEY (Sucursal_Destino_ID) REFERENCES Sucursales(Sucursal_ID)
);


-- 13. MÓDULO DE SEGURIDAD Y CONTROL DE ACCESO
-- ------------------------------------------------------------------------------
CREATE TABLE Seguridad_Cuentas (
    Cuenta_ID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Empleado_ID UNIQUEIDENTIFIER NOT NULL UNIQUE,
    Nombre_Usuario VARCHAR(50) NOT NULL UNIQUE,
    Password_Hash NVARCHAR(MAX) NOT NULL,
    Recovery_Token VARCHAR(100) NULL,
    Recovery_Token_Expiry DATETIMEOFFSET NULL,
    Security_Stamp UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
    Requiere_Cambio_Clave BIT NOT NULL DEFAULT 1,
    Intentos_Fallidos INT NOT NULL DEFAULT 0,
    Bloqueado_Hasta DATETIMEOFFSET NULL,
    Cuenta_Activa BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy VARCHAR(100) NULL,
    LastModifiedAt DATETIMEOFFSET NULL,
    LastModifiedBy VARCHAR(100) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedAt DATETIMEOFFSET NULL,
    DeletedBy VARCHAR(100) NULL,
    CONSTRAINT FK_Cuentas_Empleado FOREIGN KEY (Empleado_ID) REFERENCES Empleados(Empleado_ID)
);

CREATE TABLE Seguridad_Roles (
    Rol_ID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Nombre_Rol VARCHAR(50) NOT NULL UNIQUE,
    Descripcion VARCHAR(255) NULL,
    Nivel_Jerarquia INT NOT NULL DEFAULT 0,
    CreatedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy VARCHAR(100) NULL,
    LastModifiedAt DATETIMEOFFSET NULL,
    LastModifiedBy VARCHAR(100) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedAt DATETIMEOFFSET NULL,
    DeletedBy VARCHAR(100) NULL
);

CREATE TABLE Seguridad_Cuentas_Roles (
    Cuenta_ID UNIQUEIDENTIFIER NOT NULL,
    Rol_ID UNIQUEIDENTIFIER NOT NULL,
    AssignedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    AssignedBy VARCHAR(100) NULL,
    PRIMARY KEY (Cuenta_ID, Rol_ID),
    CONSTRAINT FK_CuentaRol_Cuenta FOREIGN KEY (Cuenta_ID) REFERENCES Seguridad_Cuentas(Cuenta_ID),
    CONSTRAINT FK_CuentaRol_Rol FOREIGN KEY (Rol_ID) REFERENCES Seguridad_Roles(Rol_ID)
);

CREATE TABLE Seguridad_Permisos_Rol (
    Permiso_ID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Rol_ID UNIQUEIDENTIFIER NOT NULL,
    Modulo_Sistema VARCHAR(50) NOT NULL,
    Accion VARCHAR(50) NOT NULL,
    CreatedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CreatedBy VARCHAR(100) NULL,
    LastModifiedAt DATETIMEOFFSET NULL,
    LastModifiedBy VARCHAR(100) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedAt DATETIMEOFFSET NULL,
    DeletedBy VARCHAR(100) NULL,
    CONSTRAINT FK_Permisos_Rol FOREIGN KEY (Rol_ID) REFERENCES Seguridad_Roles(Rol_ID),
    CONSTRAINT UQ_Rol_Modulo_Accion UNIQUE (Rol_ID, Modulo_Sistema, Accion)
);

CREATE NONCLUSTERED INDEX IX_Cuentas_Login ON Seguridad_Cuentas(Nombre_Usuario) WHERE IsDeleted = 0 AND Cuenta_Activa = 1;

-- Licencia_Prof es única solo cuando tiene valor (NULLs no compiten)
CREATE UNIQUE NONCLUSTERED INDEX UX_Empleados_LicenciaProf
    ON Empleados (Licencia_Prof)
    WHERE Licencia_Prof IS NOT NULL AND IsDeleted = 0;
