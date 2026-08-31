using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Empleados.Queries.GetById;

public record GetEmpleadoByIdQuery(Guid Id) : IRequest<Result<EmpleadoDto>>;
