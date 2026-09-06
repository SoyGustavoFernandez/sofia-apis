CREATE TABLE [dbo].[Seguridad_Roles_Sucursales] (
    [Rol_ID]        UNIQUEIDENTIFIER NOT NULL,
    [Sucursal_ID]   UNIQUEIDENTIFIER NOT NULL,
    CONSTRAINT [PK_Seguridad_Roles_Sucursales] PRIMARY KEY ([Rol_ID], [Sucursal_ID]),
    CONSTRAINT [FK_RolesSucursales_Roles]      FOREIGN KEY ([Rol_ID])      REFERENCES [dbo].[Seguridad_Roles]([Rol_ID]),
    CONSTRAINT [FK_RolesSucursales_Sucursales] FOREIGN KEY ([Sucursal_ID]) REFERENCES [dbo].[Sucursales]([Sucursal_ID])
);
