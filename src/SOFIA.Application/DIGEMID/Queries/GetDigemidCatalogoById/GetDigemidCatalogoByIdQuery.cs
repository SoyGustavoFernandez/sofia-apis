using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.DIGEMID.Queries.GetDigemidCatalogoById;

public record DigemidProductoDto(Guid Id, string CodProd, string NomProd, string? Concent, string? FormaFarmaceutica, string? Fraccion, string? RegistroSanitario, string? Titular, string Estado);

public record GetDigemidCatalogoByIdQuery(Guid Id) : IRequest<Result<DigemidProductoDto>>;
