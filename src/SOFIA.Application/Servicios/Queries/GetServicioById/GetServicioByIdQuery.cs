using SOFIA.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace SOFIA.Application.Servicios.Queries.GetServicioById;

public record GetServicioByIdQuery(Guid Id) : IRequest<Result<ServicioDto>>;

public record ServicioDto(Guid Id);
