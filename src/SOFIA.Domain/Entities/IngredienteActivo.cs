using SOFIA.Domain.Common;

namespace SOFIA.Domain.Entities;

public sealed class IngredienteActivo : BaseEntity
{
    private IngredienteActivo() { } // Required for EF Core

    public string DenominacionDci { get; private set; } = string.Empty;
    public string CodigoAtc { get; private set; } = string.Empty;

    public static Result<IngredienteActivo> Create(string denominacionDci, string codigoAtc)
    {
        if (string.IsNullOrWhiteSpace(denominacionDci))
        {
            return Result.Failure<IngredienteActivo>(Error.Validation("IngredienteActivo.DenominacionDci", "Denominación DCI is required."));
        }

        if (denominacionDci.Length > 255)
        {
            return Result.Failure<IngredienteActivo>(Error.Validation("IngredienteActivo.DenominacionDci", "Denominación DCI must not exceed 255 characters."));
        }

        if (string.IsNullOrWhiteSpace(codigoAtc))
        {
            return Result.Failure<IngredienteActivo>(Error.Validation("IngredienteActivo.CodigoAtc", "Código ATC is required."));
        }

        if (codigoAtc.Length > 15)
        {
            return Result.Failure<IngredienteActivo>(Error.Validation("IngredienteActivo.CodigoAtc", "Código ATC must not exceed 15 characters."));
        }

        return Result.Success(new IngredienteActivo
        {
            DenominacionDci = denominacionDci,
            CodigoAtc = codigoAtc
        });
    }

    public Result Update(string denominacionDci, string codigoAtc)
    {
        if (string.IsNullOrWhiteSpace(denominacionDci))
        {
            return Result.Failure(Error.Validation("IngredienteActivo.DenominacionDci", "Denominación DCI is required."));
        }

        if (denominacionDci.Length > 255)
        {
            return Result.Failure(Error.Validation("IngredienteActivo.DenominacionDci", "Denominación DCI must not exceed 255 characters."));
        }

        if (string.IsNullOrWhiteSpace(codigoAtc))
        {
            return Result.Failure(Error.Validation("IngredienteActivo.CodigoAtc", "Código ATC is required."));
        }

        if (codigoAtc.Length > 15)
        {
            return Result.Failure(Error.Validation("IngredienteActivo.CodigoAtc", "Código ATC must not exceed 15 characters."));
        }

        DenominacionDci = denominacionDci;
        CodigoAtc = codigoAtc;

        return Result.Success();
    }
}
