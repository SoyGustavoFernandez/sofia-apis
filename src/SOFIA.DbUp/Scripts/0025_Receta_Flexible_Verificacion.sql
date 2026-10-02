-- Per-company switch that makes registering simple prescriptions mandatory, who verified the prescription at checkout and the prescription number controlled products need
IF COL_LENGTH('Empresas', 'ExigirRegistroReceta') IS NULL
    ALTER TABLE Empresas ADD ExigirRegistroReceta BIT NOT NULL CONSTRAINT DF_Empresas_ExigirRegistroReceta DEFAULT 0;
GO

IF COL_LENGTH('Ventas_Cabecera', 'Empleado_Verifica_Receta_ID') IS NULL
    ALTER TABLE Ventas_Cabecera ADD Empleado_Verifica_Receta_ID UNIQUEIDENTIFIER NULL;
GO

IF COL_LENGTH('Ventas_Cabecera', 'Fecha_Verificacion_Receta') IS NULL
    ALTER TABLE Ventas_Cabecera ADD Fecha_Verificacion_Receta DATETIME NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_VentaCab_Empleado_Verifica_Receta')
    ALTER TABLE Ventas_Cabecera ADD CONSTRAINT FK_VentaCab_Empleado_Verifica_Receta FOREIGN KEY (Empleado_Verifica_Receta_ID) REFERENCES Empleados(Empleado_ID);
GO

IF COL_LENGTH('Recetas_Medicas', 'Numero_Receta') IS NULL
    ALTER TABLE Recetas_Medicas ADD Numero_Receta VARCHAR(30) NULL;
GO
