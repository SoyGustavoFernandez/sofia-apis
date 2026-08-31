using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Proveedores.Queries.GetProveedorById;

public record ProveedorDto(Guid Id, string RazonSocial, string TaxId, string? TerminosFinancieros, decimal? CalificacionEsg, decimal TasaCumplimiento);

public record GetProveedorByIdQuery(Guid Id) : IRequest<Result<ProveedorDto>>;

public class GetProveedorByIdQueryHandler(IApplicationDbContext context) : IRequestHandler<GetProveedorByIdQuery, Result<ProveedorDto>>
{
    public async Task<Result<ProveedorDto>> Handle(GetProveedorByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await context.Proveedores
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken);

        if (entity == null)
        {
            return Result.Failure<ProveedorDto>(Error.NotFound("Proveedor.NotFound", $"Proveedor with ID {request.Id} was not found."), 404);
        }

        var dto = new ProveedorDto(
            entity.Id,
            entity.RazonSocial,
            entity.TaxId,
            entity.TerminosFinancieros,
            entity.CalificacionEsg,
            entity.TasaCumplimiento);

        return Result.Success(dto);
    }
}
