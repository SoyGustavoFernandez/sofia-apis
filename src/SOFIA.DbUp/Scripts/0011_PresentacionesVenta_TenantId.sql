-- Presentaciones_Venta was created after the multi-tenant migration (0005), which only
-- backfilled TenantId onto tables that existed at that time — new tables must add it themselves.
ALTER TABLE Presentaciones_Venta ADD TenantId UNIQUEIDENTIFIER NULL;
GO
