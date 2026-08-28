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
    public Venta? Venta { get; }
    public Empleado? ProfesionalAdmn { get; }
    public Medicamento? Producto { get; }
    public LoteInventario? Lote { get; }

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
        Enums.ModalidadRegistro modalidadRegistro)
    {
        var idError = ValidateServicioIds(clienteId, profesionalAdmnId, productoId, loteId);
        if (idError is not null)
        {
            return idError;
        }

        var fieldError = ValidateServicioFields(viaAdministracion, sitioAnatomico, volumenDosis);
        if (fieldError is not null)
        {
            return fieldError;
        }

        return Result.Success(new ServicioClinicoInmunizacion
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
    }

    private static Result<ServicioClinicoInmunizacion>? ValidateServicioIds(
        Guid clienteId, Guid profesionalAdmnId, Guid productoId, Guid loteId)
    {
        if (clienteId == Guid.Empty)
        {
            return Result.Failure<ServicioClinicoInmunizacion>(Error.Validation("ServicioClinicoInmunizacion.ClienteId", "Cliente ID is required."));
        }

        if (profesionalAdmnId == Guid.Empty)
        {
            return Result.Failure<ServicioClinicoInmunizacion>(Error.Validation("ServicioClinicoInmunizacion.ProfesionalAdmnId", "Profesional Administrador ID is required."));
        }

        if (productoId == Guid.Empty)
        {
            return Result.Failure<ServicioClinicoInmunizacion>(Error.Validation("ServicioClinicoInmunizacion.ProductoId", "Producto ID is required."));
        }

        if (loteId == Guid.Empty)
        {
            return Result.Failure<ServicioClinicoInmunizacion>(Error.Validation("ServicioClinicoInmunizacion.LoteId", "Lote ID is required."));
        }

        return null;
    }

    private static Result<ServicioClinicoInmunizacion>? ValidateServicioFields(
        string viaAdministracion, string sitioAnatomico, decimal volumenDosis)
    {
        if (string.IsNullOrWhiteSpace(viaAdministracion))
        {
            return Result.Failure<ServicioClinicoInmunizacion>(Error.Validation("ServicioClinicoInmunizacion.ViaAdministracion", "Vía de administración is required."));
        }

        if (viaAdministracion.Length > 50)
        {
            return Result.Failure<ServicioClinicoInmunizacion>(Error.Validation("ServicioClinicoInmunizacion.ViaAdministracion", "Vía de administración must not exceed 50 characters."));
        }

        if (string.IsNullOrWhiteSpace(sitioAnatomico))
        {
            return Result.Failure<ServicioClinicoInmunizacion>(Error.Validation("ServicioClinicoInmunizacion.SitioAnatomico", "Sitio anatómico is required."));
        }

        if (sitioAnatomico.Length > 100)
        {
            return Result.Failure<ServicioClinicoInmunizacion>(Error.Validation("ServicioClinicoInmunizacion.SitioAnatomico", "Sitio anatómico must not exceed 100 characters."));
        }

        if (volumenDosis <= 0)
        {
            return Result.Failure<ServicioClinicoInmunizacion>(Error.Validation("ServicioClinicoInmunizacion.VolumenDosis", "Volumen de dosis must be greater than zero."));
        }

        return null;
    }

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
        var idError = ValidateServicioIds(clienteId, profesionalAdmnId, productoId, loteId);
        if (idError is not null)
        {
            return idError;
        }

        var fieldError = ValidateServicioFields(viaAdministracion, sitioAnatomico, volumenDosis);
        if (fieldError is not null)
        {
            return fieldError;
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
