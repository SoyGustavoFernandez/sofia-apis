using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Proveedores.Queries.GetProveedorById;

public record ProveedorDto(Guid Id, string RazonSocial, string TaxId, string? TerminosFinancieros, decimal? CalificacionEsg, decimal TasaCumplimiento);

public record GetProveedorByIdQuery(Guid Id) : IRequest<Result<ProveedorDto>>;
