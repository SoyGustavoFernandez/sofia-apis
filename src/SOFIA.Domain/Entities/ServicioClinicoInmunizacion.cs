using SOFIA.Domain.Common;

namespace SOFIA.Domain.Entities;

public sealed class ServicioClinicoInmunizacion : BaseEntity
{


    private ServicioClinicoInmunizacion() { } // Required for EF Core

    public Guid? VentaId { get; private set; }
    public Guid ClienteId { get; private set; }
    public Guid ProfesionalAdmnId { get; private set; }
    public Guid ProductoId { get; private set; }
    public Guid LoteId { get; private set; }
    public string ViaAdministracion { get; private set; } = string.Empty;
    public string SitioAnatomico { get; private set; } = string.Empty;
    public decimal VolumenDosis { get; private set; }
    public DateTime FechaAdmnFisica { get; private set; }
    public DateTime? FechaEntregaVis { get; private set; }
    public Enums.ModalidadRegistro ModalidadRegistro { get; private set; }

    // Navigation Properties
    public Venta? Venta { get; private set; }
    public Empleado? ProfesionalAdmn { get; private set; }
    public Medicamento? Producto { get; private set; }
    public LoteInventario? Lote { get; private set; }

    public static Result<ServicioClinicoInmunizacion> Create(
        Guid? ventaId,
        Guid clienteId,
        Guid profesionalAdmnId,
        Guid productoId,
        Guid loteId,
        string viaAdministracion,
        string sitioAnatomico,
        decimal volumenDosis,
        DateTime fechaAdmnFisica,
        DateTime? fechaEntregaVis,
        Enums.ModalidadRegistro modalidadRegistro) =>
        clienteId == Guid.Empty
            ? Result.Failure<ServicioClinicoInmunizacion>(Error.Validation("ServicioClinicoInmunizacion.ClienteId", "Cliente ID is required."))
            : profesionalAdmnId == Guid.Empty
            ? Result.Failure<ServicioClinicoInmunizacion>(Error.Validation("ServicioClinicoInmunizacion.ProfesionalAdmnId", "Profesional Administrador ID is required."))
            : productoId == Guid.Empty
            ? Result.Failure<ServicioClinicoInmunizacion>(Error.Validation("ServicioClinicoInmunizacion.ProductoId", "Producto ID is required."))
            : loteId == Guid.Empty
            ? Result.Failure<ServicioClinicoInmunizacion>(Error.Validation("ServicioClinicoInmunizacion.LoteId", "Lote ID is required."))
            : string.IsNullOrWhiteSpace(viaAdministracion)
            ? Result.Failure<ServicioClinicoInmunizacion>(Error.Validation("ServicioClinicoInmunizacion.ViaAdministracion", "Vía de administración is required."))
            : viaAdministracion.Length > 50
            ? Result.Failure<ServicioClinicoInmunizacion>(Error.Validation("ServicioClinicoInmunizacion.ViaAdministracion", "Vía de administración must not exceed 50 characters."))
            : string.IsNullOrWhiteSpace(sitioAnatomico)
            ? Result.Failure<ServicioClinicoInmunizacion>(Error.Validation("ServicioClinicoInmunizacion.SitioAnatomico", "Sitio anatómico is required."))
            : sitioAnatomico.Length > 100
            ? Result.Failure<ServicioClinicoInmunizacion>(Error.Validation("ServicioClinicoInmunizacion.SitioAnatomico", "Sitio anatómico must not exceed 100 characters."))
            : volumenDosis <= 0
            ? Result.Failure<ServicioClinicoInmunizacion>(Error.Validation("ServicioClinicoInmunizacion.VolumenDosis", "Volumen de dosis must be greater than zero."))
            : Result.Success(new ServicioClinicoInmunizacion
            {
                VentaId = ventaId == Guid.Empty ? null : ventaId,
                ClienteId = clienteId,
                ProfesionalAdmnId = profesionalAdmnId,
                ProductoId = productoId,
                LoteId = loteId,
                ViaAdministracion = viaAdministracion,
                SitioAnatomico = sitioAnatomico,
                VolumenDosis = volumenDosis,
                FechaAdmnFisica = fechaAdmnFisica,
                FechaEntregaVis = fechaEntregaVis,
                ModalidadRegistro = modalidadRegistro
            });

    public Result Update(
        Guid? ventaId,
        Guid clienteId,
        Guid profesionalAdmnId,
        Guid productoId,
        Guid loteId,
        string viaAdministracion,
        string sitioAnatomico,
        decimal volumenDosis,
        DateTime fechaAdmnFisica,
        DateTime? fechaEntregaVis,
        Enums.ModalidadRegistro modalidadRegistro)
    {
        if (clienteId == Guid.Empty)
        {
            return Result.Failure(Error.Validation("ServicioClinicoInmunizacion.ClienteId", "Cliente ID is required."));
        }

        if (profesionalAdmnId == Guid.Empty)
        {
            return Result.Failure(Error.Validation("ServicioClinicoInmunizacion.ProfesionalAdmnId", "Profesional Administrador ID is required."));
        }

        if (productoId == Guid.Empty)
        {
            return Result.Failure(Error.Validation("ServicioClinicoInmunizacion.ProductoId", "Producto ID is required."));
        }

        if (loteId == Guid.Empty)
        {
            return Result.Failure(Error.Validation("ServicioClinicoInmunizacion.LoteId", "Lote ID is required."));
        }

        if (string.IsNullOrWhiteSpace(viaAdministracion))
        {
            return Result.Failure(Error.Validation("ServicioClinicoInmunizacion.ViaAdministracion", "Vía de administración is required."));
        }

        if (viaAdministracion.Length > 50)
        {
            return Result.Failure(Error.Validation("ServicioClinicoInmunizacion.ViaAdministracion", "Vía de administración must not exceed 50 characters."));
        }

        if (string.IsNullOrWhiteSpace(sitioAnatomico))
        {
            return Result.Failure(Error.Validation("ServicioClinicoInmunizacion.SitioAnatomico", "Sitio anatómico is required."));
        }

        if (sitioAnatomico.Length > 100)
        {
            return Result.Failure(Error.Validation("ServicioClinicoInmunizacion.SitioAnatomico", "Sitio anatómico must not exceed 100 characters."));
        }

        if (volumenDosis <= 0)
        {
            return Result.Failure(Error.Validation("ServicioClinicoInmunizacion.VolumenDosis", "Volumen de dosis must be greater than zero."));
        }

        VentaId = ventaId == Guid.Empty ? null : ventaId;
        ClienteId = clienteId;
        ProfesionalAdmnId = profesionalAdmnId;
        ProductoId = productoId;
        LoteId = loteId;
        ViaAdministracion = viaAdministracion;
        SitioAnatomico = sitioAnatomico;
        VolumenDosis = volumenDosis;
        FechaAdmnFisica = fechaAdmnFisica;
        FechaEntregaVis = fechaEntregaVis;
        ModalidadRegistro = modalidadRegistro;

        return Result.Success();
    }
}
