using SOFIA.Domain.Enums;

namespace SOFIA.Application.Citas.Common;

public record CitaDto(
    Guid Id,
    Guid? PacienteId,
    string? PacienteNombre,
    string? PacienteContacto,
    Guid ServicioId,
    string ServicioNombre,
    decimal Precio,
    string SucursalNombre,
    DateTimeOffset FechaHora,
    EstadoCita Estado,
    bool AceptaRecordatorio,
    bool AplicaMedicamento,
    bool RegistraResultado,
    string? Dosis,
    string? Resultado,
    Guid? VentaId);
