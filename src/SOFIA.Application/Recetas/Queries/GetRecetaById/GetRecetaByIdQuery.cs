using SOFIA.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace SOFIA.Application.Recetas.Queries.GetRecetaById;

public record GetRecetaByIdQuery(Guid Id) : IRequest<Result<RecetaDto>>;

public record RecetaDto(Guid Id);
