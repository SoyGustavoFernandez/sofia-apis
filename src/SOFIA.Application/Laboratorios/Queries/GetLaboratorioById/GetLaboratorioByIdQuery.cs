using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Laboratorios.Queries.GetLaboratorioById;

public record GetLaboratorioByIdQuery(Guid Id) : IRequest<Result<LaboratorioDto>>;
