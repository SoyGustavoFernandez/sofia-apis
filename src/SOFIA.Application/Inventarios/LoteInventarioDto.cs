namespace SOFIA.Application.Inventarios;

public record LoteInventarioDto(
    Guid Id,
    Guid ProductoId,
    string NombreProducto,
    string NumeroLoteMfr,
    DateTimeOffset? FechaFabricacion,
    DateTimeOffset FechaCaducidad);
