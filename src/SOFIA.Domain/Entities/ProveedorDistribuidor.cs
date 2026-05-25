using SOFIA.Domain.Common;

namespace SOFIA.Domain.Entities;

public sealed class ProveedorDistribuidor : BaseEntity
{
    private ProveedorDistribuidor() { } // Required for EF Core

    public string RazonSocial { get; private set; } = string.Empty;
    public string TaxId { get; private set; } = string.Empty;
    public string? TerminosFinancieros { get; private set; }
    public decimal? CalificacionEsg { get; private set; }
    public decimal TasaCumplimiento { get; private set; }

    public static Result<ProveedorDistribuidor> Create(
        string razonSocial,
        string taxId,
        string? terminosFinancieros,
        decimal? calificacionEsg,
        decimal tasaCumplimiento = 100.00m) =>
        string.IsNullOrWhiteSpace(razonSocial)
            ? Result.Failure<ProveedorDistribuidor>(Error.Validation("ProveedorDistribuidor.RazonSocial", "Razón Social is required."))
            : razonSocial.Length > 200
            ? Result.Failure<ProveedorDistribuidor>(Error.Validation("ProveedorDistribuidor.RazonSocial", "Razón Social must not exceed 200 characters."))
            : string.IsNullOrWhiteSpace(taxId)
            ? Result.Failure<ProveedorDistribuidor>(Error.Validation("ProveedorDistribuidor.TaxId", "Tax ID is required."))
            : taxId.Length > 50
            ? Result.Failure<ProveedorDistribuidor>(Error.Validation("ProveedorDistribuidor.TaxId", "Tax ID must not exceed 50 characters."))
            : terminosFinancieros != null && terminosFinancieros.Length > 100
            ? Result.Failure<ProveedorDistribuidor>(Error.Validation("ProveedorDistribuidor.TerminosFinancieros", "Financial Terms must not exceed 100 characters."))
            : calificacionEsg.HasValue && (calificacionEsg.Value < 0.0m || calificacionEsg.Value > 100.00m)
            ? Result.Failure<ProveedorDistribuidor>(Error.Validation("ProveedorDistribuidor.CalificacionEsg", "ESG Rating must be between 0 and 100."))
            : tasaCumplimiento < 0.0m || tasaCumplimiento > 100.00m
            ? Result.Failure<ProveedorDistribuidor>(Error.Validation("ProveedorDistribuidor.TasaCumplimiento", "Compliance Rate must be between 0 and 100."))
            : Result.Success(new ProveedorDistribuidor
            {
                RazonSocial = razonSocial,
                TaxId = taxId,
                TerminosFinancieros = terminosFinancieros,
                CalificacionEsg = calificacionEsg,
                TasaCumplimiento = tasaCumplimiento
            });

    public Result Update(
        string razonSocial,
        string taxId,
        string? terminosFinancieros,
        decimal? calificacionEsg,
        decimal tasaCumplimiento)
    {
        if (string.IsNullOrWhiteSpace(razonSocial))
        {
            return Result.Failure(Error.Validation("ProveedorDistribuidor.RazonSocial", "Razón Social is required."));
        }

        if (razonSocial.Length > 200)
        {
            return Result.Failure(Error.Validation("ProveedorDistribuidor.RazonSocial", "Razón Social must not exceed 200 characters."));
        }

        if (string.IsNullOrWhiteSpace(taxId))
        {
            return Result.Failure(Error.Validation("ProveedorDistribuidor.TaxId", "Tax ID is required."));
        }

        if (taxId.Length > 50)
        {
            return Result.Failure(Error.Validation("ProveedorDistribuidor.TaxId", "Tax ID must not exceed 50 characters."));
        }

        if (terminosFinancieros != null && terminosFinancieros.Length > 100)
        {
            return Result.Failure(Error.Validation("ProveedorDistribuidor.TerminosFinancieros", "Financial Terms must not exceed 100 characters."));
        }

        if (calificacionEsg.HasValue && (calificacionEsg.Value < 0.0m || calificacionEsg.Value > 100.00m))
        {
            return Result.Failure(Error.Validation("ProveedorDistribuidor.CalificacionEsg", "ESG Rating must be between 0 and 100."));
        }

        if (tasaCumplimiento < 0.0m || tasaCumplimiento > 100.00m)
        {
            return Result.Failure(Error.Validation("ProveedorDistribuidor.TasaCumplimiento", "Compliance Rate must be between 0 and 100."));
        }

        RazonSocial = razonSocial;
        TaxId = taxId;
        TerminosFinancieros = terminosFinancieros;
        CalificacionEsg = calificacionEsg;
        TasaCumplimiento = tasaCumplimiento;

        return Result.Success();
    }
}
