using SOFIA.Domain.Common;

namespace SOFIA.Domain.Entities;

public sealed class DetalleTransferencia : BaseEntity
{
    private DetalleTransferencia() { }

    public Guid TransferenciaId { get; private set; }
    public Guid LoteId { get; private set; }
    public decimal CantidadEnviada { get; private set; }
    public decimal? CantidadRecibida { get; private set; }

    // Navigation Properties
    public Transferencia? Transferencia { get; }
    public LoteInventario? Lote { get; }

    public static Result<DetalleTransferencia> Create(
        Guid loteId,
        decimal cantidadEnviada)
    {
        if (loteId == Guid.Empty)
        {
            return Result.Failure<DetalleTransferencia>(Error.Validation("DetalleTransferencia.LoteId", "Lote ID is required."));
        }

        if (cantidadEnviada <= 0)
        {
            return Result.Failure<DetalleTransferencia>(Error.Validation("DetalleTransferencia.CantidadEnviada", "Sent quantity must be greater than zero."));
        }

        return Result.Success(new DetalleTransferencia
        {
            LoteId = loteId,
            CantidadEnviada = cantidadEnviada
        });
    }

    internal void SetTransferenciaId(Guid transferenciaId) => TransferenciaId = transferenciaId;

    internal void RegistrarRecepcion(decimal cantidadRecibida) => CantidadRecibida = cantidadRecibida;
}
