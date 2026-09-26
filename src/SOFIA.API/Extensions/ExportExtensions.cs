using SOFIA.Domain.Common;

namespace SOFIA.API.Extensions;

public static class ExportExtensions
{
    // The query already stopped at the limit, so a larger total means the file would be silently truncated
    public static bool ExceedsExportLimit<T>(this PaginatedList<T> page) => page.TotalCount > PaginationLimits.MaxPageSize;

    public static ObjectResult TooManyRowsResult() => Result.Failure(Error.Validation(
        "Export.TooManyRows",
        $"La exportación supera el máximo de {PaginationLimits.MaxPageSize} filas. Aplica filtros para reducir los resultados."))
        .ToProblemResult();
}
