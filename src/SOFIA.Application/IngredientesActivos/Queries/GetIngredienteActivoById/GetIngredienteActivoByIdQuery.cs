using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.IngredientesActivos.Queries.GetIngredienteActivoById;

public record GetIngredienteActivoByIdQuery(Guid Id) : IRequest<Result<IngredienteActivoDto>>;
