using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.POS.Commands.AperturarCaja;

public record AperturarCajaCommand(decimal MontoAperturaEfectivo) : ICommand<Guid>;
