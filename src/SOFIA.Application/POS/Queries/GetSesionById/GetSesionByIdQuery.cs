using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;
using SOFIA.Application.POS.Queries.GetSesiones;

namespace SOFIA.Application.POS.Queries.GetSesionById;

public record GetSesionByIdQuery(Guid Id) : IRequest<Result<SesionResumenDto>>;
