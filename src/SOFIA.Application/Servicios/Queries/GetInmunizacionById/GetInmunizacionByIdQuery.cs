using SOFIA.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace SOFIA.Application.Servicios.Queries.GetInmunizacionById;

public record GetInmunizacionByIdQuery(Guid Id) : IRequest<Result<InmunizacionDto>>;

public record InmunizacionDto(Guid Id);
