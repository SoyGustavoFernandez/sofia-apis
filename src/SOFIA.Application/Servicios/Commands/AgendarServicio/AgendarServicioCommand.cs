using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Servicios.Commands.AgendarServicio;

public record AgendarServicioCommand(Guid ClienteId, Guid ProductoId, Guid? VentaId, DateTime FechaHoraProgramada, string EstadoCita = "Programada") : ICommand<Guid>;
