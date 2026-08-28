using SOFIA.Domain.Common;

namespace SOFIA.Domain.Entities;

public sealed class DigemidActaDestruccion : BaseEntity
{
    private readonly List<DigemidActaDetalle> _detalles = [];

    private DigemidActaDestruccion() { }

    public string NumeroResolucionInterna { get; private set; } = string.Empty;
    public string EmpresaResiduosBiocontaminados { get; private set; } = string.Empty;
    public string? ManifiestoTransporteDoc { get; private set; }
    public DateTime FechaEjecucion { get; private set; }
    public Guid RegenteResponsableId { get; private set; }
    public string? RutaActaFirmadaPdf { get; private set; }

    // Navigation Properties
    public Empleado? RegenteResponsable { get; }
    public IReadOnlyCollection<DigemidActaDetalle> Detalles => _detalles.AsReadOnly();

    public static Result<DigemidActaDestruccion> Create(
        string numeroResolucionInterna,
        string empresaResiduosBiocontaminados,
        string? manifiestoTransporteDoc,
        DateTime fechaEjecucion,
        Guid regenteResponsableId,
        string? rutaActaFirmadaPdf)
    {
        if (string.IsNullOrWhiteSpace(numeroResolucionInterna))
        {
            return Result.Failure<DigemidActaDestruccion>(Error.Validation("DigemidActaDestruccion.NumeroResolucionInterna", "Número de Resolución Interna is required."));
        }

        if (numeroResolucionInterna.Length > 50)
        {
            return Result.Failure<DigemidActaDestruccion>(Error.Validation("DigemidActaDestruccion.NumeroResolucionInterna", "Número de Resolución Interna must not exceed 50 characters."));
        }

        if (string.IsNullOrWhiteSpace(empresaResiduosBiocontaminados))
        {
            return Result.Failure<DigemidActaDestruccion>(Error.Validation("DigemidActaDestruccion.EmpresaResiduosBiocontaminados", "Empresa de Residuos Biocontaminados is required."));
        }

        if (empresaResiduosBiocontaminados.Length > 150)
        {
            return Result.Failure<DigemidActaDestruccion>(Error.Validation("DigemidActaDestruccion.EmpresaResiduosBiocontaminados", "Empresa must not exceed 150 characters."));
        }

        if (manifiestoTransporteDoc != null && manifiestoTransporteDoc.Length > 50)
        {
            return Result.Failure<DigemidActaDestruccion>(Error.Validation("DigemidActaDestruccion.ManifiestoTransporteDoc", "Manifiesto de Transporte Doc must not exceed 50 characters."));
        }

        if (regenteResponsableId == Guid.Empty)
        {
            return Result.Failure<DigemidActaDestruccion>(Error.Validation("DigemidActaDestruccion.RegenteResponsableId", "Regente Responsable ID is required."));
        }

        if (rutaActaFirmadaPdf != null && rutaActaFirmadaPdf.Length > 500)
        {
            return Result.Failure<DigemidActaDestruccion>(Error.Validation("DigemidActaDestruccion.RutaActaFirmadaPdf", "Ruta de Acta Firmada PDF must not exceed 500 characters."));
        }

        return Result.Success(new DigemidActaDestruccion
        {
            NumeroResolucionInterna = numeroResolucionInterna,
            EmpresaResiduosBiocontaminados = empresaResiduosBiocontaminados,
            ManifiestoTransporteDoc = manifiestoTransporteDoc,
            FechaEjecucion = fechaEjecucion,
            RegenteResponsableId = regenteResponsableId,
            RutaActaFirmadaPdf = rutaActaFirmadaPdf
        });
    }

    public Result Update(
        string numeroResolucionInterna,
        string empresaResiduosBiocontaminados,
        string? manifiestoTransporteDoc,
        DateTime fechaEjecucion,
        Guid regenteResponsableId,
        string? rutaActaFirmadaPdf)
    {
        if (string.IsNullOrWhiteSpace(numeroResolucionInterna))
        {
            return Result.Failure(Error.Validation("DigemidActaDestruccion.NumeroResolucionInterna", "Número de Resolución Interna is required."));
        }

        if (numeroResolucionInterna.Length > 50)
        {
            return Result.Failure(Error.Validation("DigemidActaDestruccion.NumeroResolucionInterna", "Número de Resolución Interna must not exceed 50 characters."));
        }

        if (string.IsNullOrWhiteSpace(empresaResiduosBiocontaminados))
        {
            return Result.Failure(Error.Validation("DigemidActaDestruccion.EmpresaResiduosBiocontaminados", "Empresa de Residuos Biocontaminados is required."));
        }

        if (empresaResiduosBiocontaminados.Length > 150)
        {
            return Result.Failure(Error.Validation("DigemidActaDestruccion.EmpresaResiduosBiocontaminados", "Empresa must not exceed 150 characters."));
        }

        if (manifiestoTransporteDoc != null && manifiestoTransporteDoc.Length > 50)
        {
            return Result.Failure(Error.Validation("DigemidActaDestruccion.ManifiestoTransporteDoc", "Manifiesto de Transporte Doc must not exceed 50 characters."));
        }

        if (regenteResponsableId == Guid.Empty)
        {
            return Result.Failure(Error.Validation("DigemidActaDestruccion.RegenteResponsableId", "Regente Responsable ID is required."));
        }

        if (rutaActaFirmadaPdf != null && rutaActaFirmadaPdf.Length > 500)
        {
            return Result.Failure(Error.Validation("DigemidActaDestruccion.RutaActaFirmadaPdf", "Ruta de Acta Firmada PDF must not exceed 500 characters."));
        }

        NumeroResolucionInterna = numeroResolucionInterna;
        EmpresaResiduosBiocontaminados = empresaResiduosBiocontaminados;
        ManifiestoTransporteDoc = manifiestoTransporteDoc;
        FechaEjecucion = fechaEjecucion;
        RegenteResponsableId = regenteResponsableId;
        RutaActaFirmadaPdf = rutaActaFirmadaPdf;

        return Result.Success();
    }
}
