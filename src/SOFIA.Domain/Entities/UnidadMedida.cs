using SOFIA.Domain.Common;

namespace SOFIA.Domain.Entities;

public sealed class UnidadMedida : BaseEntity
{
    private UnidadMedida() { } // Required for EF Core

    public string Codigo { get; private set; } = string.Empty;
    public string Descripcion { get; private set; } = string.Empty;

    public static Result<UnidadMedida> Create(string codigo, string descripcion) =>
        string.IsNullOrWhiteSpace(codigo)
            ? Result.Failure<UnidadMedida>(Error.Validation("UnidadMedida.Codigo", "Codigo is required."))
            : codigo.Length > 10
            ? Result.Failure<UnidadMedida>(Error.Validation("UnidadMedida.Codigo", "Codigo must not exceed 10 characters."))
            : string.IsNullOrWhiteSpace(descripcion)
            ? Result.Failure<UnidadMedida>(Error.Validation("UnidadMedida.Descripcion", "Descripcion is required."))
            : descripcion.Length > 50
            ? Result.Failure<UnidadMedida>(Error.Validation("UnidadMedida.Descripcion", "Descripcion must not exceed 50 characters."))
            : Result.Success(new UnidadMedida
            {
                Codigo = codigo,
                Descripcion = descripcion
            });

    public Result Update(string codigo, string descripcion)
    {
        if (string.IsNullOrWhiteSpace(codigo))
        {
            return Result.Failure(Error.Validation("UnidadMedida.Codigo", "Codigo is required."));
        }

        if (codigo.Length > 10)
        {
            return Result.Failure(Error.Validation("UnidadMedida.Codigo", "Codigo must not exceed 10 characters."));
        }

        if (string.IsNullOrWhiteSpace(descripcion))
        {
            return Result.Failure(Error.Validation("UnidadMedida.Descripcion", "Descripcion is required."));
        }

        if (descripcion.Length > 50)
        {
            return Result.Failure(Error.Validation("UnidadMedida.Descripcion", "Descripcion must not exceed 50 characters."));
        }

        Codigo = codigo;
        Descripcion = descripcion;

        return Result.Success();
    }
}
