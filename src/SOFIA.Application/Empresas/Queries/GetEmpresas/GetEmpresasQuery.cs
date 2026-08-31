using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Empresas.Queries.GetEmpresas;

public record GetEmpresasQuery(EstadoEmpresa? Estado = null) : IRequest<Result<IReadOnlyList<EmpresaDto>>>;
