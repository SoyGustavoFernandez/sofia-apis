using SOFIA.Domain.Common;

namespace SOFIA.Domain.Entities;

public sealed class Medicamento : BaseEntity
{
    private Medicamento() { } // Required for EF Core

    public string CodigoNacional { get; private set; } = string.Empty;
    public string NombreComercial { get; private set; } = string.Empty;
    public Guid LaboratorioId { get; private set; }
    public Guid UnidadBaseId { get; private set; }
    public Enums.CondicionVenta CondicionVenta { get; private set; }

    // Navigation Properties
    public Laboratorio? Laboratorio { get; private set; }
    public UnidadMedida? UnidadBase { get; private set; }

    // Valid values for Condicion_Venta based on SQL CHECK constraint
    public static readonly string[] CondicionesValidas =
    [
        "Venta Libre (OTC)",
        "Receta Simple",
        "Receta Retenida",
        "Estupefaciente"
    ];

    public static Result<Medicamento> Create(
        string codigoNacional,
        string nombreComercial,
        Guid laboratorioId,
        Guid unidadBaseId,
        Enums.CondicionVenta condicionVenta)
    {
        var error = ValidateFields(codigoNacional, nombreComercial, laboratorioId, unidadBaseId);
        if (error is not null)
            return Result.Failure<Medicamento>(error);

        return Result.Success(new Medicamento
        {
            CodigoNacional = codigoNacional,
            NombreComercial = nombreComercial,
            LaboratorioId = laboratorioId,
            UnidadBaseId = unidadBaseId,
            CondicionVenta = condicionVenta
        });
    }

    public Result Update(
        string codigoNacional,
        string nombreComercial,
        Guid laboratorioId,
        Guid unidadBaseId,
        Enums.CondicionVenta condicionVenta)
    {
        var error = ValidateFields(codigoNacional, nombreComercial, laboratorioId, unidadBaseId);
        if (error is not null)
            return Result.Failure(error);

        CodigoNacional = codigoNacional;
        NombreComercial = nombreComercial;
        LaboratorioId = laboratorioId;
        UnidadBaseId = unidadBaseId;
        CondicionVenta = condicionVenta;

        return Result.Success();
    }

    private static Error? ValidateFields(
        string codigoNacional,
        string nombreComercial,
        Guid laboratorioId,
        Guid unidadBaseId)
    {
        if (string.IsNullOrWhiteSpace(codigoNacional))
            return Error.Validation("Medicamento.CodigoNacional", "Código Nacional is required.");
        if (codigoNacional.Length > 50)
            return Error.Validation("Medicamento.CodigoNacional", "Código Nacional must not exceed 50 characters.");
        if (string.IsNullOrWhiteSpace(nombreComercial))
            return Error.Validation("Medicamento.NombreComercial", "Nombre Comercial is required.");
        if (nombreComercial.Length > 150)
            return Error.Validation("Medicamento.NombreComercial", "Nombre Comercial must not exceed 150 characters.");
        if (laboratorioId == Guid.Empty)
            return Error.Validation("Medicamento.LaboratorioId", "Laboratorio ID is required.");
        if (unidadBaseId == Guid.Empty)
            return Error.Validation("Medicamento.UnidadBaseId", "Unidad Base ID is required.");
        return null;
    }
}
