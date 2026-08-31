using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Seguros.Queries.GetAseguradoraById;

public record AseguradoraDto(Guid Id, string NombreComercial, string CodigoIdentificadorNacional);

public record GetAseguradoraByIdQuery(Guid Id) : IRequest<Result<AseguradoraDto>>;
