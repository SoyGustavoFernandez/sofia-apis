using MediatR;
using SOFIA.Domain.Common;

namespace SOFIA.Application.JerarquiasUoM.Queries.GetUnidadesVendiblesUoM;

public record GetUnidadesVendiblesUoMQuery(Guid ProductoId) : IRequest<Result<List<UnidadVendibleDto>>>;

public record UnidadVendibleDto(
    Guid UnidadMedidaId,
    string Descripcion,
    decimal CantidadUnidadesBase);
