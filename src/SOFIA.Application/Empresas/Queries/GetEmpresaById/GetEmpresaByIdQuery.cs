using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Empresas.Queries.GetEmpresaById;

public record GetEmpresaByIdQuery(Guid Id) : IRequest<Result<EmpresaDto>>;
