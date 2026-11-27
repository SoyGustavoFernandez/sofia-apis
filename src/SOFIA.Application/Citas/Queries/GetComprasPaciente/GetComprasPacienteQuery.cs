using MediatR;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Citas.Queries.GetComprasPaciente;

// Medicines the patient already bought, to apply one without charging or deducting it again
public record GetComprasPacienteQuery(Guid PacienteId) : IRequest<Result<List<CompraPacienteDto>>>;

public record CompraPacienteDto(Guid VentaId, DateTime FechaHora, Guid LoteId, string ProductoNombre, string NumeroLote);
