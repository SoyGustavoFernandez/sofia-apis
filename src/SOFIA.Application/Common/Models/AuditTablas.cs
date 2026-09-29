namespace SOFIA.Application.Common.Models;

/// <summary>Audited table names as stored in Auditoria_Eventos_Seguridad.Tabla_Afectada.</summary>
public static class AuditTablas
{
    public const string Ventas = "Ventas_Cabecera";
    public const string Devoluciones = "Devoluciones_Cabecera";
    public const string Inventario = "Inventario_Sucursal";
    public const string SesionesCaja = "POS_Sesiones_Caja";
    public const string Permisos = "Seguridad_Permisos_Rol";
    public const string Roles = "Seguridad_Roles";
    public const string Cuentas = "Seguridad_Cuentas";
    public const string SeriesFiscales = "SUNAT_Series_Fiscales";
    public const string Empresas = "Empresas";
}
