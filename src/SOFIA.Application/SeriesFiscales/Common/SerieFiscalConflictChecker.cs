using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Extensions;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.Application.SeriesFiscales.Common;

public static class SerieFiscalConflictChecker
{
    public static async Task<Result> CheckAsync(
        IApplicationDbContext context,
        Guid? serieId,
        Guid sucursalId,
        TipoComprobante tipo,
        string prefijo,
        string estado,
        CancellationToken cancellationToken)
    {
        var sucursalExiste = await context.Sucursales.AnyAsync(s => s.Id == sucursalId && !s.IsDeleted, cancellationToken);
        if (!sucursalExiste)
        {
            return Result.Failure(Error.NotFound("Sucursal.NotFound", "The specified branch does not exist."), 404);
        }

        var prefijoDuplicado = await context.SUNATSeriesFiscales
            .AnyAsync(s => s.Id != serieId && s.TipoComprobante == tipo && s.PrefijoSerie == prefijo && !s.IsDeleted, cancellationToken);
        if (prefijoDuplicado)
        {
            return Result.Failure(Error.Conflict("SerieFiscal.PrefijoDuplicado", "Another series of this document type already uses this prefix."), 409);
        }

        if (estado != SunatSerieFiscal.EstadoActiva)
        {
            return Result.Success();
        }

        var otraActiva = await context.SUNATSeriesFiscales
            .AnyAsync(s => s.Id != serieId && s.SucursalId == sucursalId && s.TipoComprobante == tipo && s.EstadoSerie == SunatSerieFiscal.EstadoActiva && !s.IsDeleted, cancellationToken);

        return otraActiva
            ? Result.Failure(Error.Conflict("SerieFiscal.ActivaDuplicada", "The branch already has an active series of this document type."), 409)
            : Result.Success();
    }

    // Maps a unique index race lost after the pre-check to the same conflict the pre-check returns
    public static Error? MapUniqueViolation(DbUpdateException exception)
    {
        if (exception.IsUniqueIndexViolation(SerieFiscalIndexes.PrefijoPorTipo))
        {
            return Error.Conflict("SerieFiscal.PrefijoDuplicado", "Another series of this document type already uses this prefix.");
        }

        return exception.IsUniqueIndexViolation(SerieFiscalIndexes.ActivaPorSucursalTipo)
            ? Error.Conflict("SerieFiscal.ActivaDuplicada", "The branch already has an active series of this document type.")
            : null;
    }
}
