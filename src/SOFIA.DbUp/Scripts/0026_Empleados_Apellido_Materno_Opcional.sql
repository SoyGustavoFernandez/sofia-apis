-- Employee second surname becomes optional: a person with a single surname stores NULL instead of a placeholder
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Empleados') AND name = 'Apellido_Materno' AND is_nullable = 0)
    ALTER TABLE Empleados ALTER COLUMN Apellido_Materno VARCHAR(75) NULL;
GO
