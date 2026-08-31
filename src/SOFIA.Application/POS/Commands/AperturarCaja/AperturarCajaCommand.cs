using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.POS.Commands.AperturarCaja;

public record AperturarCajaCommand(Guid SucursalId, Guid EmpleadoId, DateTime FechaHoraApertura, decimal MontoAperturaEfectivo) : ICommand<Guid>;
