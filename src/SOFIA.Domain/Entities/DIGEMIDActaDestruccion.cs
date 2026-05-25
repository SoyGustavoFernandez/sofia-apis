using SOFIA.Domain.Common;

namespace SOFIA.Domain.Entities;

public sealed class DIGEMIDActaDestruccion : BaseEntity
{
    private readonly List<DIGEMIDActaDetalle> _detalles = [];

    private DIGEMIDActaDestruccion() { }

    public string NumeroResolucionInterna { get; private set; } = string.Empty;
    public string EmpresaResiduosBiocontaminados { get; private set; } = string.Empty;
    public string? ManifiestoTransporteDoc { get; private set; }
    public DateTime FechaEjecucion { get; private set; }
    public Guid RegenteResponsableId { get; private set; }
    public string? RutaActaFirmadaPdf { get; private set; }

    // Navigation Properties
    public Empleado? RegenteResponsable { get; private set; }
    public IReadOnlyCollection<DIGEMIDActaDetalle> Detalles => _detalles.AsReadOnly();

    public static Result<DIGEMIDActaDestruccion> Create(
        string numeroResolucionInterna,
        string empresaResiduosBiocontaminados,
        string? manifiestoTransporteDoc,
        DateTime fechaEjecucion,
        Guid regenteResponsableId,
        string? rutaActaFirmadaPdf)
    {
        if (string.IsNullOrWhiteSpace(numeroResolucionInterna))
        {
            return Result.Failure<DIGEMIDActaDestruccion>(Error.Validation("DIGEMIDActaDestruccion.NumeroResolucionInterna", "Número de Resolución Interna is required."));
        }

        if (numeroResolucionInterna.Length > 50)
        {
            return Result.Failure<DIGEMIDActaDestruccion>(Error.Validation("DIGEMIDActaDestruccion.NumeroResolucionInterna", "Número de Resolución Interna must not exceed 50 characters."));
        }

        if (string.IsNullOrWhiteSpace(empresaResiduosBiocontaminados))
        {
            return Result.Failure<DIGEMIDActaDestruccion>(Error.Validation("DIGEMIDActaDestruccion.EmpresaResiduosBiocontaminados", "Empresa de Residuos Biocontaminados is required."));
        }

        if (empresaResiduosBiocontaminados.Length > 150)
        {
            return Result.Failure<DIGEMIDActaDestruccion>(Error.Validation("DIGEMIDActaDestruccion.EmpresaResiduosBiocontaminados", "Empresa must not exceed 150 characters."));
        }

        if (manifiestoTransporteDoc != null && manifiestoTransporteDoc.Length > 50)
        {
            return Result.Failure<DIGEMIDActaDestruccion>(Error.Validation("DIGEMIDActaDestruccion.ManifiestoTransporteDoc", "Manifiesto de Transporte Doc must not exceed 50 characters."));
        }

        if (regenteResponsableId == Guid.Empty)
        {
            return Result.Failure<DIGEMIDActaDestruccion>(Error.Validation("DIGEMIDActaDestruccion.RegenteResponsableId", "Regente Responsable ID is required."));
        }

        if (rutaActaFirmadaPdf != null && rutaActaFirmadaPdf.Length > 500)
        {
            return Result.Failure<DIGEMIDActaDestruccion>(Error.Validation("DIGEMIDActaDestruccion.RutaActaFirmadaPdf", "Ruta de Acta Firmada PDF must not exceed 500 characters."));
        }

        return Result.Success(new DIGEMIDActaDestruccion
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
            return Result.Failure(Error.Validation("DIGEMIDActaDestruccion.NumeroResolucionInterna", "Número de Resolución Interna is required."));
        }

        if (numeroResolucionInterna.Length > 50)
        {
            return Result.Failure(Error.Validation("DIGEMIDActaDestruccion.NumeroResolucionInterna", "Número de Resolución Interna must not exceed 50 characters."));
        }

        if (string.IsNullOrWhiteSpace(empresaResiduosBiocontaminados))
        {
            return Result.Failure(Error.Validation("DIGEMIDActaDestruccion.EmpresaResiduosBiocontaminados", "Empresa de Residuos Biocontaminados is required."));
        }

        if (empresaResiduosBiocontaminados.Length > 150)
        {
            return Result.Failure(Error.Validation("DIGEMIDActaDestruccion.EmpresaResiduosBiocontaminados", "Empresa must not exceed 150 characters."));
        }

        if (manifiestoTransporteDoc != null && manifiestoTransporteDoc.Length > 50)
        {
            return Result.Failure(Error.Validation("DIGEMIDActaDestruccion.ManifiestoTransporteDoc", "Manifiesto de Transporte Doc must not exceed 50 characters."));
        }

        if (regenteResponsableId == Guid.Empty)
        {
            return Result.Failure(Error.Validation("DIGEMIDActaDestruccion.RegenteResponsableId", "Regente Responsable ID is required."));
        }

        if (rutaActaFirmadaPdf != null && rutaActaFirmadaPdf.Length > 500)
        {
            return Result.Failure(Error.Validation("DIGEMIDActaDestruccion.RutaActaFirmadaPdf", "Ruta de Acta Firmada PDF must not exceed 500 characters."));
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
