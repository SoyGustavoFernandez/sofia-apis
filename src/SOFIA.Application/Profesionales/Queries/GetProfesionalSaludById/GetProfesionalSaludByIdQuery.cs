using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Profesionales.Queries.GetProfesionalSaludById;

public record ProfesionalSaludDto(Guid Id, string NumeroRegistro, string NombrePrescriptor, string? DireccionClinica);

public record GetProfesionalSaludByIdQuery(Guid Id) : IRequest<Result<ProfesionalSaludDto>>;
