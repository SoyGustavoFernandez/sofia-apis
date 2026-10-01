-- Patient identity document type as a SUNAT catálogo 06 code (1 DNI, 4 CE, 6 RUC, 7 Pasaporte); NULL keeps patients registered before it existed
IF COL_LENGTH('Pacientes_Clientes', 'Tipo_Doc_Identidad') IS NULL
    ALTER TABLE Pacientes_Clientes ADD Tipo_Doc_Identidad VARCHAR(1) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_Pacientes_Clientes_Tipo_Doc_Identidad')
    ALTER TABLE Pacientes_Clientes ADD CONSTRAINT CHK_Pacientes_Clientes_Tipo_Doc_Identidad CHECK (Tipo_Doc_Identidad IN ('1', '4', '6', '7'));
GO
