using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.DIGEMID.Commands.AislarLoteCuarentena;

public record AislarLoteCuarentenaCommand(Guid LoteId, Guid? DetalleDevId, decimal CantidadAislada, string MotivoAislamiento, string EstadoResolucion) : ICommand<Guid>;
