using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Magistrales.Commands.IniciarOrdenMagistral;

public record InsumoDto(Guid InventarioSucursalId, decimal CantidadConsumida);

public record IniciarOrdenMagistralCommand(
    Guid SucursalId,
    Guid? RecetaId,
    Guid ProductoResultanteId,
    Guid QuimicoPreparadorId,
    decimal? CantidadProducida,
    List<InsumoDto> Consumos
) : ICommand<Guid>;
