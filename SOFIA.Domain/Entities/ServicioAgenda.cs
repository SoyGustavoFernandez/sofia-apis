using SOFIA.Domain.Common;

namespace SOFIA.Domain.Entities;

public sealed class ServicioAgenda : BaseEntity
{
    private ServicioAgenda() { } // Required for EF Core

    public Guid ClienteId { get; private set; }
    public Guid ProductoId { get; private set; }
    public Guid? VentaId { get; private set; }
    public DateTime FechaHoraProgramada { get; private set; }
    public string EstadoCita { get; private set; } = "Programada";

    // Navigation Properties
    public Medicamento? Producto { get; private set; }
    public Venta? Venta { get; private set; }

    public static Result<ServicioAgenda> Create(
        Guid clienteId,
        Guid productoId,
        Guid? ventaId,
        DateTime fechaHoraProgramada,
        string estadoCita = "Programada") =>
        clienteId == Guid.Empty
            ? Result.Failure<ServicioAgenda>(Error.Validation("ServicioAgenda.ClienteId", "Cliente ID is required."))
            : productoId == Guid.Empty
            ? Result.Failure<ServicioAgenda>(Error.Validation("ServicioAgenda.ProductoId", "Producto ID is required."))
            : string.IsNullOrWhiteSpace(estadoCita)
            ? Result.Failure<ServicioAgenda>(Error.Validation("ServicioAgenda.EstadoCita", "Estado de cita is required."))
            : estadoCita.Length > 20
            ? Result.Failure<ServicioAgenda>(Error.Validation("ServicioAgenda.EstadoCita", "Estado de cita must not exceed 20 characters."))
            : Result.Success(new ServicioAgenda
            {
                ClienteId = clienteId,
                ProductoId = productoId,
                VentaId = ventaId == Guid.Empty ? null : ventaId,
                FechaHoraProgramada = fechaHoraProgramada,
                EstadoCita = estadoCita
            });

    public Result Update(
        Guid clienteId,
        Guid productoId,
        Guid? ventaId,
        DateTime fechaHoraProgramada,
        string estadoCita)
    {
        if (clienteId == Guid.Empty)
        {
            return Result.Failure(Error.Validation("ServicioAgenda.ClienteId", "Cliente ID is required."));
        }

        if (productoId == Guid.Empty)
        {
            return Result.Failure(Error.Validation("ServicioAgenda.ProductoId", "Producto ID is required."));
        }

        if (string.IsNullOrWhiteSpace(estadoCita))
        {
            return Result.Failure(Error.Validation("ServicioAgenda.EstadoCita", "Estado de cita is required."));
        }

        if (estadoCita.Length > 20)
        {
            return Result.Failure(Error.Validation("ServicioAgenda.EstadoCita", "Estado de cita must not exceed 20 characters."));
        }

        ClienteId = clienteId;
        ProductoId = productoId;
        VentaId = ventaId == Guid.Empty ? null : ventaId;
        FechaHoraProgramada = fechaHoraProgramada;
        EstadoCita = estadoCita;

        return Result.Success();
    }
}
