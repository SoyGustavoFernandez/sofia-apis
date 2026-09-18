-- Supports the POS top-sellers query (sucursal + last-30-days + estado), which otherwise scans Ventas_Cabecera in full.
CREATE INDEX [IX_Ventas_Cabecera_Sucursal_Fecha_Estado] ON [dbo].[Ventas_Cabecera] ([Sucursal_ID], [Fecha_Hora_UTC], [Estado]);
