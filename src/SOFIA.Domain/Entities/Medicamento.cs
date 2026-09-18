using System.ComponentModel;
using System.Reflection;
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
    // Nullable: this field was added after many medicamentos already existed with no price on
    // file. New/edited records always carry a real value — see ValidateFields.
    public decimal? PrecioVentaBase { get; private set; }

    // Navigation Properties
    public Laboratorio? Laboratorio { get; }
    public UnidadMedida? UnidadBase { get; }

    public const int CodigoNacionalMaxLength = 50;
    public const int NombreComercialMaxLength = 150;

    public static readonly string[] CondicionesValidas =
        [.. Enum.GetValues<Enums.CondicionVenta>()
            .Select(e => typeof(Enums.CondicionVenta)
                .GetField(e.ToString())!
                .GetCustomAttribute<DescriptionAttribute>()?.Description ?? e.ToString())];

    public static Result<Medicamento> Create(
        string codigoNacional,
        string nombreComercial,
        Guid laboratorioId,
        Guid unidadBaseId,
        Enums.CondicionVenta condicionVenta,
        decimal? precioVentaBase = null)
    {
        var error = ValidateFields(codigoNacional, nombreComercial, laboratorioId, unidadBaseId, precioVentaBase);
        if (error is not null)
        {
            return Result.Failure<Medicamento>(error);
        }

        return Result.Success(new Medicamento
        {
            CodigoNacional = codigoNacional,
            NombreComercial = nombreComercial,
            LaboratorioId = laboratorioId,
            UnidadBaseId = unidadBaseId,
            CondicionVenta = condicionVenta,
            PrecioVentaBase = precioVentaBase
        });
    }

    public Result Update(
        string codigoNacional,
        string nombreComercial,
        Guid laboratorioId,
        Guid unidadBaseId,
        Enums.CondicionVenta condicionVenta,
        decimal? precioVentaBase = null)
    {
        var error = ValidateFields(codigoNacional, nombreComercial, laboratorioId, unidadBaseId, precioVentaBase);
        if (error is not null)
        {
            return Result.Failure(error);
        }

        CodigoNacional = codigoNacional;
        NombreComercial = nombreComercial;
        LaboratorioId = laboratorioId;
        UnidadBaseId = unidadBaseId;
        CondicionVenta = condicionVenta;
        PrecioVentaBase = precioVentaBase;

        return Result.Success();
    }

    private static Error? ValidateFields(
        string codigoNacional,
        string nombreComercial,
        Guid laboratorioId,
        Guid unidadBaseId,
        decimal? precioVentaBase = null)
    {
        if (string.IsNullOrWhiteSpace(codigoNacional))
        {
            return Error.Validation("Medicamento.CodigoNacional", "Código Nacional is required.");
        }

        if (codigoNacional.Length > CodigoNacionalMaxLength)
        {
            return Error.Validation("Medicamento.CodigoNacional", $"Código Nacional must not exceed {CodigoNacionalMaxLength} characters.");
        }

        if (string.IsNullOrWhiteSpace(nombreComercial))
        {
            return Error.Validation("Medicamento.NombreComercial", "Nombre Comercial is required.");
        }

        if (nombreComercial.Length > NombreComercialMaxLength)
        {
            return Error.Validation("Medicamento.NombreComercial", $"Nombre Comercial must not exceed {NombreComercialMaxLength} characters.");
        }

        if (laboratorioId == Guid.Empty)
        {
            return Error.Validation("Medicamento.LaboratorioId", "Laboratorio ID is required.");
        }

        if (unidadBaseId == Guid.Empty)
        {
            return Error.Validation("Medicamento.UnidadBaseId", "Unidad Base ID is required.");
        }

        if (precioVentaBase is < 0)
        {
            return Error.Validation("Medicamento.PrecioVentaBase", "Precio de Venta Base cannot be negative.");
        }

        return null;
    }
}
