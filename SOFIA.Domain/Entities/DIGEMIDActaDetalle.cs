using SOFIA.Domain.Common;

namespace SOFIA.Domain.Entities;

public sealed class DIGEMIDActaDetalle : BaseEntity
{
    private DIGEMIDActaDetalle() { }

    public Guid ActaId { get; private set; }
    public Guid RegistroCuarentenaId { get; private set; }
    public decimal CantidadDestruida { get; private set; }

    // Navigation Properties
    public DIGEMIDActaDestruccion? Acta { get; private set; }
    public DIGEMIDInventarioCuarentena? RegistroCuarentena { get; private set; }

    public static Result<DIGEMIDActaDetalle> Create(
        Guid actaId,
        Guid registroCuarentenaId,
        decimal cantidadDestruida)
    {
        if (actaId == Guid.Empty)
        {
            return Result.Failure<DIGEMIDActaDetalle>(Error.Validation("DIGEMIDActaDetalle.ActaId", "Acta ID is required."));
        }

        if (registroCuarentenaId == Guid.Empty)
        {
            return Result.Failure<DIGEMIDActaDetalle>(Error.Validation("DIGEMIDActaDetalle.RegistroCuarentenaId", "Registro Cuarentena ID is required."));
        }

        if (cantidadDestruida <= 0)
        {
            return Result.Failure<DIGEMIDActaDetalle>(Error.Validation("DIGEMIDActaDetalle.CantidadDestruida", "Cantidad Destruida must be greater than zero."));
        }

        return Result.Success(new DIGEMIDActaDetalle
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
            return Result.Failure(Error.Validation("DIGEMIDActaDetalle.ActaId", "Acta ID is required."));
        }

        if (registroCuarentenaId == Guid.Empty)
        {
            return Result.Failure(Error.Validation("DIGEMIDActaDetalle.RegistroCuarentenaId", "Registro Cuarentena ID is required."));
        }

        if (cantidadDestruida <= 0)
        {
            return Result.Failure(Error.Validation("DIGEMIDActaDetalle.CantidadDestruida", "Cantidad Destruida must be greater than zero."));
        }

        ActaId = actaId;
        RegistroCuarentenaId = registroCuarentenaId;
        CantidadDestruida = cantidadDestruida;

        return Result.Success();
    }
}
