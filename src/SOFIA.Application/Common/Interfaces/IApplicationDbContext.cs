using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DatabaseFacade Database { get; }
    DbSet<Empresa> Empresas { get; }
    DbSet<Sucursal> Sucursales { get; }
    DbSet<Empleado> Empleados { get; }
    DbSet<UnidadMedida> UnidadesMedida { get; }
    DbSet<Laboratorio> Laboratorios { get; }
    DbSet<IngredienteActivo> IngredientesActivos { get; }
    DbSet<Medicamento> Medicamentos { get; }
    DbSet<JerarquiaUoM> JerarquiasUoM { get; }
    DbSet<FormulacionClinica> FormulacionesClinicas { get; }
    DbSet<Cuenta> Cuentas { get; }
    DbSet<Rol> Roles { get; }
    DbSet<PermisoRol> PermisosRol { get; }
    DbSet<CuentaRol> CuentasRoles { get; }
    DbSet<CuentaSucursal> CuentasSucursales { get; }
    DbSet<RolSucursal> RolesSucursales { get; }
    DbSet<LoteInventario> LotesInventario { get; }
    DbSet<InventarioSucursal> LotesEnSucursal { get; }
    DbSet<Venta> Ventas { get; }
    DbSet<DetalleVenta> DetallesVenta { get; }
    DbSet<Transferencia> Transferencias { get; }
    DbSet<DetalleTransferencia> DetallesTransferencia { get; }
    DbSet<ProveedorDistribuidor> Proveedores { get; }
    DbSet<HistorialPrecioProveedor> HistorialPreciosProveedor { get; }
    DbSet<PacienteCliente> Pacientes { get; }
    DbSet<ProfesionalSalud> ProfesionalesSalud { get; }
    DbSet<RecetaMedica> Recetas { get; }
    DbSet<PosSesionCaja> POSSesionesCaja { get; }
    DbSet<VentaPago> VentasPagos { get; }
    DbSet<AseguradoraMedica> Aseguradoras { get; }
    DbSet<VentaReclamoSeguro> VentasReclamosSeguro { get; }
    DbSet<DespachoDelivery> DespachosDelivery { get; }
    DbSet<ServicioClinicoInmunizacion> ServiciosClinicosInmunizacion { get; }
    DbSet<ServicioAgenda> ServiciosAgenda { get; }
    DbSet<DevolucionCabecera> Devoluciones { get; }
    DbSet<DevolucionDetalle> DetallesDevolucion { get; }
    DbSet<DigemidCatalogoProducto> DigemidCatalogoProductos { get; }
    DbSet<DigemidInventarioCuarentena> DigemidInventarioCuarentena { get; }
    DbSet<DigemidActaDestruccion> DIGEMIDActasDestruccion { get; }
    DbSet<DigemidActaDetalle> DIGEMIDActasDetalle { get; }
    DbSet<MagistralOrdenProduccion> MagistralesOrdenesProduccion { get; }
    DbSet<MagistralConsumoInsumo> MagistralesConsumosInsumo { get; }
    DbSet<SunatSerieFiscal> SUNATSeriesFiscales { get; }
    DbSet<SunatComprobanteEmitido> SUNATComprobantesEmitidos { get; }
    DbSet<AuditoriaEventoSeguridad> AuditoriasEventosSeguridad { get; }
    DbSet<SistemaOutboxEvento> SistemaOutboxEventos { get; }
    DbSet<RegistroPrivacidadPresidio> RegistrosPrivacidadPresidio { get; }
    DbSet<RecetaDigitalizadaIA> RecetasDigitalizadasIA { get; }
    DbSet<SistemaNotificacionInterna> SistemaNotificacionesInternas { get; }
    DbSet<RefreshToken> RefreshTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
