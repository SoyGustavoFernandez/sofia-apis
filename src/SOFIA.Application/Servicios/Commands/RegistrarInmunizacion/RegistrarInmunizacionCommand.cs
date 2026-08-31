using SOFIA.Domain.Enums;
using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Servicios.Commands.RegistrarInmunizacion;

public record RegistrarInmunizacionCommand(Guid? VentaId, Guid ClienteId, Guid ProfesionalAdmnId, Guid ProductoId, Guid LoteId, string ViaAdministracion, string SitioAnatomico, decimal VolumenDosis, DateTime FechaAdmnFisica, DateTime? FechaEntregaVis, ModalidadRegistro ModalidadRegistro) : ICommand<Guid>;
