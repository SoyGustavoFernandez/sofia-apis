using Microsoft.EntityFrameworkCore;

namespace SOFIA.Application.Common.Models;

public class PaginatedList<T>(IReadOnlyCollection<T> items, int count, int pageNumber, int pageSize)
{
    public IReadOnlyCollection<T> Items { get; } = items;
    public int PageNumber { get; } = PaginationLimits.NormalizePageNumber(pageNumber);
    public int TotalPages { get; } = (int)Math.Ceiling(count / (double)PaginationLimits.NormalizePageSize(pageSize));
    public int TotalCount { get; } = count;

    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    public static async Task<PaginatedList<T>> CreateAsync(IQueryable<T> source, int pageNumber, int pageSize)
    {
        pageNumber = PaginationLimits.NormalizePageNumber(pageNumber);
        pageSize = PaginationLimits.NormalizePageSize(pageSize);

        // Computed in long so a huge page number yields an empty page instead of an overflowed negative offset
        var skip = (int)Math.Min((long)(pageNumber - 1) * pageSize, int.MaxValue);

        var count = await source.CountAsync();
        var items = await source.Skip(skip).Take(pageSize).ToListAsync();

        return new PaginatedList<T>(items, count, pageNumber, pageSize);
    }
}
