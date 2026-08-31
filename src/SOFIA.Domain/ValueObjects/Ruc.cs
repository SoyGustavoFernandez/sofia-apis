using SOFIA.Domain.Common;

namespace SOFIA.Domain.ValueObjects;

public sealed record Ruc
{
    public string Value { get; }

    private Ruc(string value) => Value = value;

    public static Result<Ruc> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<Ruc>(Error.Validation("Ruc.Vacio", "RUC is required."));
        }

        if (value.Length != 11 || !value.All(char.IsDigit))
        {
            return Result.Failure<Ruc>(Error.Validation("Ruc.Invalido", "RUC must be 11 numeric digits."));
        }

        return Result.Success(new Ruc(value));
    }

    internal static Ruc From(string value) => new(value);

    public override string ToString() => Value;
}
