using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Extensions;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Onboarding.Commands.RegistrarPrimerProducto;

public class RegistrarPrimerProductoCommandHandler(IApplicationDbContext context, ICurrentUser currentUser)
    : IRequestHandler<RegistrarPrimerProductoCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(RegistrarPrimerProductoCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsInRole(Rol.AdminRoleName))
        {
            return Result.Failure<Guid>(OnboardingErrors.SoloAdministrador, 403);
        }

        var sucursalResult = currentUser.GetSucursalId();
        if (sucursalResult.IsFailure)
        {
            return Result.Failure<Guid>(sucursalResult.Error);
        }

        var codigo = request.CodigoNacional.Trim();
        if (await context.Medicamentos.AnyAsync(m => m.CodigoNacional == codigo && !m.IsDeleted, cancellationToken))
        {
            return Result.Failure<Guid>(Error.Conflict("Medicamento.CodigoNacional.Duplicado", "Another product already uses this national code."), 409);
        }

        // Tenant-filtered lookup: a unit of another company must not be stored as a reference
        if (!await context.UnidadesMedida.AnyAsync(u => u.Id == request.UnidadBaseId, cancellationToken))
        {
            return Result.Failure<Guid>(Error.NotFound("UnidadMedida.NotFound", "The specified unit of measure does not exist."), 404);
        }

        var laboratorioResult = await ResolveLaboratorioAsync(request.LaboratorioNombre.Trim(), cancellationToken);
        if (laboratorioResult.IsFailure)
        {
            return Result.Failure<Guid>(laboratorioResult.Error);
        }

        var productoResult = Medicamento.Create(
            codigo, request.NombreComercial.Trim(), laboratorioResult.Value, request.UnidadBaseId, request.CondicionVenta, request.PrecioVentaBase);
        if (productoResult.IsFailure)
        {
            return Result.Failure<Guid>(productoResult.Error);
        }

        var producto = productoResult.Value!;

        var loteResult = LoteInventario.Create(producto.Id, request.NumeroLote.Trim(), null, request.FechaCaducidad);
        if (loteResult.IsFailure)
        {
            return Result.Failure<Guid>(loteResult.Error);
        }

        var lote = loteResult.Value!;

        var stockResult = InventarioSucursal.Create(sucursalResult.Value, lote.Id, request.Cantidad);
        if (stockResult.IsFailure)
        {
            return Result.Failure<Guid>(stockResult.Error);
        }

        // One save for everything, so a failure never leaves a product without stock behind
        _ = context.Medicamentos.Add(producto);
        _ = context.LotesInventario.Add(lote);
        _ = context.LotesEnSucursal.Add(stockResult.Value!);
        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success(producto.Id, 201);
    }

    // Reuses the laboratory with the same name; a new one is only added to the change tracker
    private async Task<Result<Guid>> ResolveLaboratorioAsync(string nombre, CancellationToken cancellationToken)
    {
        var existente = await context.Laboratorios.FirstOrDefaultAsync(l => l.NombreCompania == nombre, cancellationToken);
        if (existente is not null)
        {
            return Result.Success(existente.Id);
        }

        var nuevoResult = Laboratorio.Create(nombre, null);
        if (nuevoResult.IsFailure)
        {
            return Result.Failure<Guid>(nuevoResult.Error);
        }

        _ = context.Laboratorios.Add(nuevoResult.Value!);
        return Result.Success(nuevoResult.Value!.Id);
    }
}
