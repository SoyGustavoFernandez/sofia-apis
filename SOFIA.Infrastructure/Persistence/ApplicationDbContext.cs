using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Entities;
using System.Reflection;

namespace SOFIA.Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentUser currentUser)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<Sucursal> Sucursales => Set<Sucursal>();
    public DbSet<Empleado> Empleados => Set<Empleado>();
    public DbSet<UnidadMedida> UnidadesMedida => Set<UnidadMedida>();
    public DbSet<Laboratorio> Laboratorios => Set<Laboratorio>();
    public DbSet<IngredienteActivo> IngredientesActivos => Set<IngredienteActivo>();
    public DbSet<Medicamento> Medicamentos => Set<Medicamento>();
    public DbSet<JerarquiaUoM> JerarquiasUoM => Set<JerarquiaUoM>();
    public DbSet<FormulacionClinica> FormulacionesClinicas => Set<FormulacionClinica>();
    public DbSet<Cuenta> Cuentas => Set<Cuenta>();
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<PermisoRol> PermisosRol => Set<PermisoRol>();
    public DbSet<CuentaRol> CuentasRoles => Set<CuentaRol>();
    public DbSet<LoteInventario> LotesInventario => Set<LoteInventario>();
    public DbSet<InventarioSucursal> LotesEnSucursal => Set<InventarioSucursal>();
    public DbSet<Venta> Ventas => Set<Venta>();
    public DbSet<DetalleVenta> DetallesVenta => Set<DetalleVenta>();
    public DbSet<Transferencia> Transferencias => Set<Transferencia>();
    public DbSet<DetalleTransferencia> DetallesTransferencia => Set<DetalleTransferencia>();
    public DbSet<ProveedorDistribuidor> Proveedores => Set<ProveedorDistribuidor>();
    public DbSet<HistorialPrecioProveedor> HistorialPreciosProveedor => Set<HistorialPrecioProveedor>();
    public DbSet<PacienteCliente> Pacientes => Set<PacienteCliente>();
    public DbSet<ProfesionalSalud> ProfesionalesSalud => Set<ProfesionalSalud>();
    public DbSet<RecetaMedica> Recetas => Set<RecetaMedica>();
    public DbSet<POSSesionCaja> POSSesionesCaja => Set<POSSesionCaja>();
    public DbSet<VentaPago> VentasPagos => Set<VentaPago>();
    public DbSet<AseguradoraMedica> Aseguradoras => Set<AseguradoraMedica>();
    public DbSet<VentaReclamoSeguro> VentasReclamosSeguro => Set<VentaReclamoSeguro>();
    public DbSet<DespachoDelivery> DespachosDelivery => Set<DespachoDelivery>();
    public DbSet<ServicioClinicoInmunizacion> ServiciosClinicosInmunizacion => Set<ServicioClinicoInmunizacion>();
    public DbSet<ServicioAgenda> ServiciosAgenda => Set<ServicioAgenda>();
    public DbSet<DevolucionCabecera> Devoluciones => Set<DevolucionCabecera>();
    public DbSet<DevolucionDetalle> DetallesDevolucion => Set<DevolucionDetalle>();
    public DbSet<DIGEMIDCatalogoProducto> DIGEMIDCatalogoProductos => Set<DIGEMIDCatalogoProducto>();
    public DbSet<DIGEMIDInventarioCuarentena> DIGEMIDInventarioCuarentena => Set<DIGEMIDInventarioCuarentena>();
    public DbSet<DIGEMIDActaDestruccion> DIGEMIDActasDestruccion => Set<DIGEMIDActaDestruccion>();
    public DbSet<DIGEMIDActaDetalle> DIGEMIDActasDetalle => Set<DIGEMIDActaDetalle>();
    public DbSet<MagistralOrdenProduccion> MagistralesOrdenesProduccion => Set<MagistralOrdenProduccion>();
    public DbSet<MagistralConsumoInsumo> MagistralesConsumosInsumo => Set<MagistralConsumoInsumo>();
    public DbSet<SUNATSerieFiscal> SUNATSeriesFiscales => Set<SUNATSerieFiscal>();
    public DbSet<SUNATComprobanteEmitido> SUNATComprobantesEmitidos => Set<SUNATComprobanteEmitido>();
    public DbSet<AuditoriaEventoSeguridad> AuditoriasEventosSeguridad => Set<AuditoriaEventoSeguridad>();
    public DbSet<SistemaOutboxEvento> SistemaOutboxEventos => Set<SistemaOutboxEvento>();
    public DbSet<RegistroPrivacidadPresidio> RegistrosPrivacidadPresidio => Set<RegistroPrivacidadPresidio>();
    public DbSet<RecetaDigitalizadaIA> RecetasDigitalizadasIA => Set<RecetaDigitalizadaIA>();
    public DbSet<SistemaNotificacionInterna> SistemaNotificacionesInternas => Set<SistemaNotificacionInterna>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<Domain.Common.IAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedBy = currentUser.Name ?? "SYSTEM";
                    entry.Entity.CreatedAt = DateTimeOffset.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.LastModifiedBy = currentUser.Name ?? "SYSTEM";
                    entry.Entity.LastModifiedAt = DateTimeOffset.UtcNow;
                    break;
                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    entry.Entity.DeletedBy = currentUser.Name ?? "SYSTEM";
                    entry.Entity.DeletedAt = DateTimeOffset.UtcNow;
                    entry.Entity.IsDeleted = true;
                    break;
                case EntityState.Detached:
                    break;
                case EntityState.Unchanged:
                    break;
                default:
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
