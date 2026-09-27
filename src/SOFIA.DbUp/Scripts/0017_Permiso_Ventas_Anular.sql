-- Voiding a sale gets its own permission; every role that could void through Ventas:Actualizar keeps that ability
INSERT INTO Seguridad_Permisos_Rol (Rol_ID, Modulo_Sistema, Accion, TenantId, CreatedBy)
SELECT p.Rol_ID, p.Modulo_Sistema, 'Anular', p.TenantId, 'DbUp'
FROM Seguridad_Permisos_Rol p
WHERE p.Modulo_Sistema = 'Ventas' AND p.Accion = 'Actualizar' AND p.IsDeleted = 0
  AND NOT EXISTS (
      SELECT 1 FROM Seguridad_Permisos_Rol x
      WHERE x.Rol_ID = p.Rol_ID AND x.Modulo_Sistema = p.Modulo_Sistema AND x.Accion = 'Anular');
GO
