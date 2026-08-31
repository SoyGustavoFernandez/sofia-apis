using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.UnidadesMedida.Queries.GetUnidadMedidaById;

public record GetUnidadMedidaByIdQuery(Guid Id) : IRequest<Result<UnidadMedidaDto>>;
