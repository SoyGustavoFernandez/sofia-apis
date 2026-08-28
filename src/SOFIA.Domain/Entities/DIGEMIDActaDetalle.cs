using SOFIA.Domain.Common;

namespace SOFIA.Domain.Entities;

public sealed class DigemidActaDetalle : BaseEntity
{
    private DigemidActaDetalle() { }

    public Guid ActaId { get; private set; }
    public Guid RegistroCuarentenaId { get; private set; }
    public decimal CantidadDestruida { get; private set; }

    // Navigation Properties
    public DigemidActaDestruccion? Acta { get; }
    public DigemidInventarioCuarentena? RegistroCuarentena { get; }

    public static Result<DigemidActaDetalle> Create(
        Guid actaId,
        Guid registroCuarentenaId,
        decimal cantidadDestruida)
    {
        if (actaId == Guid.Empty)
        {
            return Result.Failure<DigemidActaDetalle>(Error.Validation("DigemidActaDetalle.ActaId", "Acta ID is required."));
        }

        if (registroCuarentenaId == Guid.Empty)
        {
            return Result.Failure<DigemidActaDetalle>(Error.Validation("DigemidActaDetalle.RegistroCuarentenaId", "Registro Cuarentena ID is required."));
        }

        if (cantidadDestruida <= 0)
        {
            return Result.Failure<DigemidActaDetalle>(Error.Validation("DigemidActaDetalle.CantidadDestruida", "Cantidad Destruida must be greater than zero."));
        }

        return Result.Success(new DigemidActaDetalle
        {
            ActaId = actaId,
            RegistroCuarentenaId = registroCuarentenaId,
            CantidadDestruida = cantidadDestruida
        });
    }

    public Result Update(
        Guid actaId,
        Guid registroCuarentenaId,
        decimal cantidadDestruida)
    {
        if (actaId == Guid.Empty)
        {
            return Result.Failure(Error.Validation("DigemidActaDetalle.ActaId", "Acta ID is required."));
        }

        if (registroCuarentenaId == Guid.Empty)
        {
            return Result.Failure(Error.Validation("DigemidActaDetalle.RegistroCuarentenaId", "Registro Cuarentena ID is required."));
        }

        if (cantidadDestruida <= 0)
        {
            return Result.Failure(Error.Validation("DigemidActaDetalle.CantidadDestruida", "Cantidad Destruida must be greater than zero."));
        }

        ActaId = actaId;
        RegistroCuarentenaId = registroCuarentenaId;
        CantidadDestruida = cantidadDestruida;

        return Result.Success();
    }
}
