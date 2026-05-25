namespace SOFIA.Application.Transferencias;

public record TransferenciaDto(
    Guid Id,
    Guid SucursalOrigenId,
    string SucursalOrigenNombre,
    Guid SucursalDestinoId,
    string SucursalDestinoNombre,
    string EstadoLogistico,
    Guid EmpleadoEmisorId,
    string EmpleadoEmisorNombre,
    Guid? EmpleadoReceptorId,
    string? EmpleadoReceptorNombre,
    DateTime FechaDespacho,
    DateTime? FechaRecepcion,
    List<DetalleTransferenciaDto> Detalles);
