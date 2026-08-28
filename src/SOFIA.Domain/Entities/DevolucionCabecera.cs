using SOFIA.Domain.Common;

namespace SOFIA.Domain.Entities;

public sealed class DevolucionCabecera : BaseEntity
{
    private readonly List<DevolucionDetalle> _detalles = [];

    private DevolucionCabecera() { } // Required for EF Core

    public Guid ComprobanteOrigenId { get; private set; }
    public Guid? ComprobanteNcId { get; private set; }
    public Guid EmpleadoAutorizaId { get; private set; }
    public string MotivoSunatCatalogo { get; private set; } = string.Empty;
    public string SustentoDescriptivo { get; private set; } = string.Empty;
    public DateTime FechaDevolucion { get; private set; }

    // Navigation Properties
    public Empleado? EmpleadoAutoriza { get; }
    public IReadOnlyCollection<DevolucionDetalle> Detalles => _detalles.AsReadOnly();

    public static Result<DevolucionCabecera> Create(
        Guid comprobanteOrigenId,
        Guid? comprobanteNcId,
        Guid empleadoAutorizaId,
        string motivoSunatCatalogo,
        string sustentoDescriptivo,
        DateTime fechaDevolucion,
        List<DevolucionDetalle> detalles)
    {
        if (comprobanteOrigenId == Guid.Empty)
        {
            return Result.Failure<DevolucionCabecera>(Error.Validation("DevolucionCabecera.ComprobanteOrigenId", "Comprobante de origen ID is required."));
        }

        if (empleadoAutorizaId == Guid.Empty)
        {
            return Result.Failure<DevolucionCabecera>(Error.Validation("DevolucionCabecera.EmpleadoAutorizaId", "Empleado que autoriza ID is required."));
        }

        if (string.IsNullOrWhiteSpace(motivoSunatCatalogo))
        {
            return Result.Failure<DevolucionCabecera>(Error.Validation("DevolucionCabecera.MotivoSunatCatalogo", "Motivo SUNAT del catálogo is required."));
        }

        if (motivoSunatCatalogo.Length != 2)
        {
            return Result.Failure<DevolucionCabecera>(Error.Validation("DevolucionCabecera.MotivoSunatCatalogo", "Motivo SUNAT del catálogo must be exactly 2 characters."));
        }

        if (string.IsNullOrWhiteSpace(sustentoDescriptivo))
        {
            return Result.Failure<DevolucionCabecera>(Error.Validation("DevolucionCabecera.SustentoDescriptivo", "Sustento descriptivo is required."));
        }

        if (sustentoDescriptivo.Length > 255)
        {
            return Result.Failure<DevolucionCabecera>(Error.Validation("DevolucionCabecera.SustentoDescriptivo", "Sustento descriptivo must not exceed 255 characters."));
        }

        if (detalles == null || detalles.Count == 0)
        {
            return Result.Failure<DevolucionCabecera>(Error.Validation("DevolucionCabecera.Detalles", "A return must contain at least one detail line."));
        }

        var cabecera = new DevolucionCabecera
        {
            ComprobanteOrigenId = comprobanteOrigenId,
            ComprobanteNcId = comprobanteNcId == Guid.Empty ? null : comprobanteNcId,
            EmpleadoAutorizaId = empleadoAutorizaId,
            MotivoSunatCatalogo = motivoSunatCatalogo,
            SustentoDescriptivo = sustentoDescriptivo,
            FechaDevolucion = fechaDevolucion
        };

        foreach (var detalle in detalles)
        {
            detalle.SetDevolucionId(cabecera.Id);
            cabecera._detalles.Add(detalle);
        }

        return Result.Success(cabecera);
    }

    public Result Update(
        Guid comprobanteOrigenId,
        Guid? comprobanteNcId,
        Guid empleadoAutorizaId,
        string motivoSunatCatalogo,
        string sustentoDescriptivo,
        DateTime fechaDevolucion)
    {
        if (comprobanteOrigenId == Guid.Empty)
        {
            return Result.Failure(Error.Validation("DevolucionCabecera.ComprobanteOrigenId", "Comprobante de origen ID is required."));
        }

        if (empleadoAutorizaId == Guid.Empty)
        {
            return Result.Failure(Error.Validation("DevolucionCabecera.EmpleadoAutorizaId", "Empleado que autoriza ID is required."));
        }

        if (string.IsNullOrWhiteSpace(motivoSunatCatalogo))
        {
            return Result.Failure(Error.Validation("DevolucionCabecera.MotivoSunatCatalogo", "Motivo SUNAT del catálogo is required."));
        }

        if (motivoSunatCatalogo.Length != 2)
        {
            return Result.Failure(Error.Validation("DevolucionCabecera.MotivoSunatCatalogo", "Motivo SUNAT del catálogo must be exactly 2 characters."));
        }

        if (string.IsNullOrWhiteSpace(sustentoDescriptivo))
        {
            return Result.Failure(Error.Validation("DevolucionCabecera.SustentoDescriptivo", "Sustento descriptivo is required."));
        }

        if (sustentoDescriptivo.Length > 255)
        {
            return Result.Failure(Error.Validation("DevolucionCabecera.SustentoDescriptivo", "Sustento descriptivo must not exceed 255 characters."));
        }

        ComprobanteOrigenId = comprobanteOrigenId;
        ComprobanteNcId = comprobanteNcId == Guid.Empty ? null : comprobanteNcId;
        EmpleadoAutorizaId = empleadoAutorizaId;
        MotivoSunatCatalogo = motivoSunatCatalogo;
        SustentoDescriptivo = sustentoDescriptivo;
        FechaDevolucion = fechaDevolucion;

        return Result.Success();
    }
}
