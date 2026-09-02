using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Entities;

namespace SOFIA.Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentUser currentUser)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<Empresa> Empresas => Set<Empresa>();
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
    public DbSet<PosSesionCaja> POSSesionesCaja => Set<PosSesionCaja>();
    public DbSet<VentaPago> VentasPagos => Set<VentaPago>();
    public DbSet<AseguradoraMedica> Aseguradoras => Set<AseguradoraMedica>();
    public DbSet<VentaReclamoSeguro> VentasReclamosSeguro => Set<VentaReclamoSeguro>();
    public DbSet<DespachoDelivery> DespachosDelivery => Set<DespachoDelivery>();
    public DbSet<ServicioClinicoInmunizacion> ServiciosClinicosInmunizacion => Set<ServicioClinicoInmunizacion>();
    public DbSet<ServicioAgenda> ServiciosAgenda => Set<ServicioAgenda>();
    public DbSet<DevolucionCabecera> Devoluciones => Set<DevolucionCabecera>();
    public DbSet<DevolucionDetalle> DetallesDevolucion => Set<DevolucionDetalle>();
    public DbSet<DigemidCatalogoProducto> DigemidCatalogoProductos => Set<DigemidCatalogoProducto>();
    public DbSet<DigemidInventarioCuarentena> DigemidInventarioCuarentena => Set<DigemidInventarioCuarentena>();
    public DbSet<DigemidActaDestruccion> DIGEMIDActasDestruccion => Set<DigemidActaDestruccion>();
    public DbSet<DigemidActaDetalle> DIGEMIDActasDetalle => Set<DigemidActaDetalle>();
    public DbSet<MagistralOrdenProduccion> MagistralesOrdenesProduccion => Set<MagistralOrdenProduccion>();
    public DbSet<MagistralConsumoInsumo> MagistralesConsumosInsumo => Set<MagistralConsumoInsumo>();
    public DbSet<SunatSerieFiscal> SUNATSeriesFiscales => Set<SunatSerieFiscal>();
    public DbSet<SunatComprobanteEmitido> SUNATComprobantesEmitidos => Set<SunatComprobanteEmitido>();
    public DbSet<AuditoriaEventoSeguridad> AuditoriasEventosSeguridad => Set<AuditoriaEventoSeguridad>();
    public DbSet<SistemaOutboxEvento> SistemaOutboxEventos => Set<SistemaOutboxEvento>();
    public DbSet<RegistroPrivacidadPresidio> RegistrosPrivacidadPresidio => Set<RegistroPrivacidadPresidio>();
    public DbSet<RecetaDigitalizadaIA> RecetasDigitalizadasIA => Set<RecetaDigitalizadaIA>();
    public DbSet<SistemaNotificacionInterna> SistemaNotificacionesInternas => Set<SistemaNotificacionInterna>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    private Guid? CurrentEmpresaId =>
        Guid.TryParse(currentUser.EmpresaId, out var id) ? id : null;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
            .Where(e => typeof(Domain.Common.BaseEntity).IsAssignableFrom(e.ClrType) && !e.IsOwned()))
        {
            ApplyTenantFilter(modelBuilder, entityType.ClrType);
        }

        base.OnModelCreating(modelBuilder);
    }

    private void ApplyTenantFilter(ModelBuilder modelBuilder, Type clrType)
    {
        var parameter = Expression.Parameter(clrType, "e");

        var notDeleted = Expression.Not(Expression.Property(parameter, nameof(Domain.Common.BaseEntity.IsDeleted)));
        var tenantId = Expression.Property(parameter, nameof(Domain.Common.BaseEntity.TenantId));
        var nullGuid = Expression.Constant(null, typeof(Guid?));

        Expression<Func<Guid?>> captureEmpresaId = () => CurrentEmpresaId;
        var empresaIdExpr = captureEmpresaId.Body;

        var tenantIsNull = Expression.Equal(tenantId, nullGuid);
        var empresaIsNull = Expression.Equal(empresaIdExpr, nullGuid);
        var tenantsMatch = Expression.Equal(tenantId, empresaIdExpr);
        var tenantCheck = Expression.OrElse(tenantIsNull, Expression.OrElse(empresaIsNull, tenantsMatch));

        var filter = Expression.Lambda(Expression.AndAlso(notDeleted, tenantCheck), parameter);
        _ = modelBuilder.Entity(clrType).HasQueryFilter(filter);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Persist domain events as outbox entries before saving
        var domainEvents = ChangeTracker.Entries<Domain.Common.BaseEntity>()
            .SelectMany(e => e.Entity.DomainEvents)
            .ToList();

        foreach (var domainEvent in domainEvents)
        {
            var outboxResult = SistemaOutboxEvento.Create(
                domainEvent.GetType().FullName!,
                JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
                false, null, null);

            if (outboxResult.IsSuccess)
                SistemaOutboxEventos.Add(outboxResult.Value);
        }

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

        foreach (var entry in ChangeTracker.Entries<Domain.Common.BaseEntity>())
            entry.Entity.ClearDomainEvents();

        return await base.SaveChangesAsync(cancellationToken);
    }
}
