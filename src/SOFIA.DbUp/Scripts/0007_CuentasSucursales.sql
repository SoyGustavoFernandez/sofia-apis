-- ------------------------------------------------------------------------------
-- 1. TABLE: Seguridad_CuentasSucursales
-- ------------------------------------------------------------------------------
CREATE TABLE [dbo].[Seguridad_CuentasSucursales] (
    [CuentaId]      UNIQUEIDENTIFIER    NOT NULL,
    [SucursalId]    UNIQUEIDENTIFIER    NOT NULL,
    [Assigned_At]   DATETIMEOFFSET      NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [Assigned_By]   NVARCHAR(100)       NULL,
    CONSTRAINT [PK_Seguridad_CuentasSucursales] PRIMARY KEY ([CuentaId], [SucursalId]),
    CONSTRAINT [FK_CuentasSucursales_Cuentas]    FOREIGN KEY ([CuentaId])   REFERENCES [dbo].[Seguridad_Cuentas]([Cuenta_ID]),
    CONSTRAINT [FK_CuentasSucursales_Sucursales] FOREIGN KEY ([SucursalId]) REFERENCES [dbo].[Sucursales]([Sucursal_ID])
);