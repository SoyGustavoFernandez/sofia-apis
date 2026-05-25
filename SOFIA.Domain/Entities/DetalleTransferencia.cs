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
    public Transferencia? Transferencia { get; private set; }
    public LoteInventario? Lote { get; private set; }

    public static Result<DetalleTransferencia> Create(
        Guid loteId,
        decimal cantidadEnviada) =>
        loteId == Guid.Empty
            ? Result.Failure<DetalleTransferencia>(Error.Validation("DetalleTransferencia.LoteId", "Lote ID is required."))
            : cantidadEnviada <= 0
                ? Result.Failure<DetalleTransferencia>(Error.Validation("DetalleTransferencia.CantidadEnviada", "Sent quantity must be greater than zero."))
                : Result.Success(new DetalleTransferencia
                {
                    LoteId = loteId,
                    CantidadEnviada = cantidadEnviada
                });

    internal void SetTransferenciaId(Guid transferenciaId) => TransferenciaId = transferenciaId;

    internal void RegistrarRecepcion(decimal cantidadRecibida) => CantidadRecibida = cantidadRecibida;
}
