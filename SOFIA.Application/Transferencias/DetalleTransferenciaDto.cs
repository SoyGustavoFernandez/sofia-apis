namespace SOFIA.Application.Transferencias;

public record DetalleTransferenciaDto(
    Guid Id,
    Guid LoteId,
    string NumeroLote,
    decimal CantidadEnviada,
    decimal? CantidadRecibida);
