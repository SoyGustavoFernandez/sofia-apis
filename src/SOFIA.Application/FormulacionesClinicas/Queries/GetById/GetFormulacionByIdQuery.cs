using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.FormulacionesClinicas.Queries.GetById;

public record GetFormulacionByIdQuery(Guid Id) : IRequest<Result<FormulacionClinicaDto>>;
