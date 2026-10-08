using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Proveedores.Commands.RegistrarPrecioProveedor;

public class RegistrarPrecioProveedorCommandHandler(IApplicationDbContext dbContext) : IRequestHandler<RegistrarPrecioProveedorCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(RegistrarPrecioProveedorCommand request, CancellationToken cancellationToken)
    {
        var proveedorExists = await dbContext.Proveedores.AnyAsync(x => x.Id == request.ProveedorId, cancellationToken);
        if (!proveedorExists)
        {
            return Result.Failure<Guid>(Error.NotFound("Proveedor.NotFound", $"Proveedor with ID {request.ProveedorId} was not found."), 404);
        }

        var medicamentoExists = await dbContext.Medicamentos.AnyAsync(x => x.Id == request.MedicamentoId, cancellationToken);
        if (!medicamentoExists)
        {
            return Result.Failure<Guid>(Error.NotFound("Medicamento.NotFound", $"Medicamento with ID {request.MedicamentoId} was not found."), 404);
        }

        var ahora = DateTime.UtcNow;
        var costoNeto = Math.Round(request.PrecioCompra * (1m - (request.DescuentoPorcentaje / 100m)), 4, MidpointRounding.AwayFromZero);

        var createResult = HistorialPrecioProveedor.Create(request.ProveedorId, request.MedicamentoId, costoNeto, ahora, null, 0);
        if (createResult.IsFailure)
        {
            return Result.Failure<Guid>(createResult.Error);
        }

        // One open price per supplier and product: the previous one is closed at the same instant the new one starts
        var abiertos = await dbContext.HistorialPreciosProveedor
            .Where(h => h.ProveedorId == request.ProveedorId && h.ProductoId == request.MedicamentoId && h.FechaFinVigencia == null)
            .ToListAsync(cancellationToken);

        foreach (var abierto in abiertos)
        {
            var closeResult = abierto.CerrarVigencia(ahora);
            if (closeResult.IsFailure)
            {
                return Result.Failure<Guid>(closeResult.Error);
            }
        }

        var entity = createResult.Value!;
        _ = dbContext.HistorialPreciosProveedor.Add(entity);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(entity.Id);
    }
}
