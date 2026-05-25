using SOFIA.Domain.Common;
using SOFIA.Domain.Enums;

namespace SOFIA.Domain.Entities;

public sealed class Transferencia : BaseEntity
{
    private readonly List<DetalleTransferencia> _detalles = [];

    private Transferencia() { }

    public Guid SucursalOrigenId { get; private set; }
    public Guid SucursalDestinoId { get; private set; }
    public EstadoLogistico EstadoLogistico { get; private set; }
    public Guid EmpleadoEmisorId { get; private set; }
    public Guid? EmpleadoReceptorId { get; private set; }
    public DateTime FechaDespacho { get; private set; }
    public DateTime? FechaRecepcion { get; private set; }

    // Navigation Properties
    public Sucursal? SucursalOrigen { get; private set; }
    public Sucursal? SucursalDestino { get; private set; }
    public Empleado? EmpleadoEmisor { get; private set; }
    public Empleado? EmpleadoReceptor { get; private set; }

    public IReadOnlyCollection<DetalleTransferencia> Detalles => _detalles.AsReadOnly();

    public static Result<Transferencia> Create(
        Guid sucursalOrigenId,
        Guid sucursalDestinoId,
        Guid empleadoEmisorId,
        List<DetalleTransferencia> detalles)
    {
        if (sucursalOrigenId == Guid.Empty)
        {
            return Result.Failure<Transferencia>(Error.Validation("Transferencia.SucursalOrigenId", "Sucursal de origen es requerida."));
        }

        if (sucursalDestinoId == Guid.Empty)
        {
            return Result.Failure<Transferencia>(Error.Validation("Transferencia.SucursalDestinoId", "Sucursal de destino es requerida."));
        }

        if (sucursalOrigenId == sucursalDestinoId)
        {
            return Result.Failure<Transferencia>(Error.Validation("Transferencia.SucursalDestinoId", "La sucursal de destino debe ser diferente de la sucursal de origen."));
        }

        if (empleadoEmisorId == Guid.Empty)
        {
            return Result.Failure<Transferencia>(Error.Validation("Transferencia.EmpleadoEmisorId", "Empleado emisor es requerido."));
        }

        if (detalles == null || detalles.Count == 0)
        {
            return Result.Failure<Transferencia>(Error.Validation("Transferencia.Detalles", "La transferencia debe contener al menos un detalle."));
        }

        var transferencia = new Transferencia
        {
            SucursalOrigenId = sucursalOrigenId,
            SucursalDestinoId = sucursalDestinoId,
            EmpleadoEmisorId = empleadoEmisorId,
            EstadoLogistico = EstadoLogistico.Iniciada,
            FechaDespacho = DateTime.UtcNow // Se inicializa a la fecha actual para cumplir con NOT NULL
        };

        foreach (var detalle in detalles)
        {
            detalle.SetTransferenciaId(transferencia.Id);
            transferencia._detalles.Add(detalle);
        }

        return Result.Success(transferencia);
    }

    public Result Aprobar()
    {
        if (EstadoLogistico != EstadoLogistico.Iniciada)
        {
            return Result.Failure(Error.Validation("Transferencia.EstadoLogistico", "Solo se pueden aprobar transferencias en estado Iniciada."));
        }

        EstadoLogistico = EstadoLogistico.Aprobada;
        return Result.Success();
    }

    public Result Despachar()
    {
        if (EstadoLogistico is not (EstadoLogistico.Iniciada or EstadoLogistico.Aprobada))
        {
            return Result.Failure(Error.Validation("Transferencia.EstadoLogistico", "Solo se pueden despachar transferencias en estado Iniciada o Aprobada."));
        }

        EstadoLogistico = EstadoLogistico.En_Transito;
        FechaDespacho = DateTime.UtcNow;
        return Result.Success();
    }

    public Result Recibir(Guid empleadoReceptorId, List<(Guid LoteId, decimal CantidadRecibida)> recepciones)
    {
        if (EstadoLogistico != EstadoLogistico.En_Transito)
        {
            return Result.Failure(Error.Validation("Transferencia.EstadoLogistico", "Solo se pueden recibir transferencias que estén En Tránsito."));
        }

        if (empleadoReceptorId == Guid.Empty)
        {
            return Result.Failure(Error.Validation("Transferencia.EmpleadoReceptorId", "Empleado receptor es requerido."));
        }

        EmpleadoReceptorId = empleadoReceptorId;
        FechaRecepcion = DateTime.UtcNow;

        var tieneRecepcionParcial = false;

        foreach (var (loteId, cantidadRecibida) in recepciones)
        {
            var detalle = _detalles.FirstOrDefault(d => d.LoteId == loteId);
            if (detalle == null)
            {
                return Result.Failure(Error.Validation("Transferencia.Recepcion", $"El lote {loteId} no pertenece a esta transferencia."));
            }

            if (cantidadRecibida < 0)
            {
                return Result.Failure(Error.Validation("Transferencia.Recepcion", "La cantidad recibida no puede ser negativa."));
            }

            if (cantidadRecibida > detalle.CantidadEnviada)
            {
                return Result.Failure(Error.Validation("Transferencia.Recepcion", $"La cantidad recibida ({cantidadRecibida}) no puede superar la cantidad enviada ({detalle.CantidadEnviada}) para el lote {loteId}."));
            }

            detalle.RegistrarRecepcion(cantidadRecibida);

            if (cantidadRecibida < detalle.CantidadEnviada)
            {
                tieneRecepcionParcial = true;
            }
        }

        EstadoLogistico = tieneRecepcionParcial ? EstadoLogistico.Recibida_Parcial : EstadoLogistico.Completada;

        return Result.Success();
    }

    public Result Cancelar()
    {
        if (EstadoLogistico is EstadoLogistico.Completada or EstadoLogistico.Recibida_Parcial or EstadoLogistico.Cancelada)
        {
            return Result.Failure(Error.Validation("Transferencia.EstadoLogistico", $"No se puede cancelar una transferencia en estado {EstadoLogistico}."));
        }

        EstadoLogistico = EstadoLogistico.Cancelada;
        return Result.Success();
    }
}
