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
        Enums.CondicionVenta condicionVenta) =>
        string.IsNullOrWhiteSpace(codigoNacional)
            ? Result.Failure<Medicamento>(Error.Validation("Medicamento.CodigoNacional", "Código Nacional is required."))
            : codigoNacional.Length > 50
            ? Result.Failure<Medicamento>(Error.Validation("Medicamento.CodigoNacional", "Código Nacional must not exceed 50 characters."))
            : string.IsNullOrWhiteSpace(nombreComercial)
            ? Result.Failure<Medicamento>(Error.Validation("Medicamento.NombreComercial", "Nombre Comercial is required."))
            : nombreComercial.Length > 150
            ? Result.Failure<Medicamento>(Error.Validation("Medicamento.NombreComercial", "Nombre Comercial must not exceed 150 characters."))
            : laboratorioId == Guid.Empty
            ? Result.Failure<Medicamento>(Error.Validation("Medicamento.LaboratorioId", "Laboratorio ID is required."))
            : unidadBaseId == Guid.Empty
            ? Result.Failure<Medicamento>(Error.Validation("Medicamento.UnidadBaseId", "Unidad Base ID is required."))
            : Result.Success(new Medicamento
            {
                CodigoNacional = codigoNacional,
                NombreComercial = nombreComercial,
                LaboratorioId = laboratorioId,
                UnidadBaseId = unidadBaseId,
                CondicionVenta = condicionVenta
            });

    public Result Update(
        string codigoNacional,
        string nombreComercial,
        Guid laboratorioId,
        Guid unidadBaseId,
        Enums.CondicionVenta condicionVenta)
    {
        if (string.IsNullOrWhiteSpace(codigoNacional))
        {
            return Result.Failure(Error.Validation("Medicamento.CodigoNacional", "Código Nacional is required."));
        }

        if (codigoNacional.Length > 50)
        {
            return Result.Failure(Error.Validation("Medicamento.CodigoNacional", "Código Nacional must not exceed 50 characters."));
        }

        if (string.IsNullOrWhiteSpace(nombreComercial))
        {
            return Result.Failure(Error.Validation("Medicamento.NombreComercial", "Nombre Comercial is required."));
        }

        if (nombreComercial.Length > 150)
        {
            return Result.Failure(Error.Validation("Medicamento.NombreComercial", "Nombre Comercial must not exceed 150 characters."));
        }

        if (laboratorioId == Guid.Empty)
        {
            return Result.Failure(Error.Validation("Medicamento.LaboratorioId", "Laboratorio ID is required."));
        }

        if (unidadBaseId == Guid.Empty)
        {
            return Result.Failure(Error.Validation("Medicamento.UnidadBaseId", "Unidad Base ID is required."));
        }

        CodigoNacional = codigoNacional;
        NombreComercial = nombreComercial;
        LaboratorioId = laboratorioId;
        UnidadBaseId = unidadBaseId;
        CondicionVenta = condicionVenta;

        return Result.Success();
    }
}
