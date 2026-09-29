namespace SOFIA.Application.Common.Models;

/// <summary>Stable security audit event types stored in Auditoria_Eventos_Seguridad.Tipo_Accion (max 20 chars).</summary>
public static class AuditEventos
{
    public const string VentaAnular = "VENTA_ANULAR";
    public const string DevolucionProcesar = "DEVOLUCION_PROCESAR";
    public const string StockAjustar = "STOCK_AJUSTAR";
    public const string CajaAperturar = "CAJA_APERTURAR";
    public const string CajaCerrar = "CAJA_CERRAR";
    public const string PermisoAsignar = "PERMISO_ASIGNAR";
    public const string PermisoRevocar = "PERMISO_REVOCAR";
    public const string RolAsignarCuenta = "ROL_ASIGNAR_CUENTA";
    public const string RolQuitarCuenta = "ROL_QUITAR_CUENTA";
    public const string RolSucursales = "ROL_SUCURSALES";
    public const string RolCrear = "ROL_CREAR";
    public const string RolActualizar = "ROL_ACTUALIZAR";
    public const string RolEliminar = "ROL_ELIMINAR";
    public const string CuentaRegistrar = "CUENTA_REGISTRAR";
    public const string CuentaActualizar = "CUENTA_ACTUALIZAR";
    public const string CuentaEliminar = "CUENTA_ELIMINAR";
    public const string CuentaSucursalAsignar = "CUENTA_SUC_ASIGNAR";
    public const string CuentaSucursalQuitar = "CUENTA_SUC_QUITAR";
    public const string ClaveCambiar = "CLAVE_CAMBIAR";
    public const string ClaveRestablecer = "CLAVE_RESTABLECER";
    public const string SerieCrear = "SERIE_CREAR";
    public const string SerieActualizar = "SERIE_ACTUALIZAR";
    public const string SerieEliminar = "SERIE_ELIMINAR";
    public const string EmpresaActualizar = "EMPRESA_ACTUALIZAR";
}
