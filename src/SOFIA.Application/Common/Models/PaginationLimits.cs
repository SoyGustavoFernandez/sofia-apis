namespace SOFIA.Application.Common.Models;

public static class PaginationLimits
{
    // Excel exports reuse the list queries and build the whole workbook in memory, so this caps them too
    public const int MaxPageSize = 10_000;

    public static int NormalizePageNumber(int pageNumber) => Math.Max(pageNumber, 1);

    public static int NormalizePageSize(int pageSize) => Math.Clamp(pageSize, 1, MaxPageSize);
}
