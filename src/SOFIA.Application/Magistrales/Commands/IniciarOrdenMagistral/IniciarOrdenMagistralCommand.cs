using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Magistrales.Commands.IniciarOrdenMagistral;

public record InsumoDto(Guid InventarioSucursalId, decimal CantidadConsumida);

public record IniciarOrdenMagistralCommand(
    Guid? RecetaId,
    Guid ProductoResultanteId,
    decimal? CantidadProducida,
    List<InsumoDto> Consumos
) : ICommand<Guid>;
