using System.Linq.Expressions;

namespace SOFIA.Application.Common.Extensions;

public static class QueryableExtensions
{
    // Bounds are normalized to UTC and sent as parameters so the comparison never depends on the server time zone
    public static IQueryable<T> WhereDateRange<T>(
        this IQueryable<T> source,
        Expression<Func<T, DateTime>> selector,
        DateTime? from,
        DateTime? to)
    {
        if (from.HasValue)
        {
            var fromUtc = ToUtc(from.Value);
            Expression<Func<DateTime>> fromAccessor = () => fromUtc;
            var param = selector.Parameters[0];
            var body = Expression.GreaterThanOrEqual(selector.Body, fromAccessor.Body);
            source = source.Where(Expression.Lambda<Func<T, bool>>(body, param));
        }

        if (to.HasValue)
        {
            var toUtc = ToUtc(to.Value);
            Expression<Func<DateTime>> toAccessor = () => toUtc;
            var param = selector.Parameters[0];
            var body = Expression.LessThanOrEqual(selector.Body, toAccessor.Body);
            source = source.Where(Expression.Lambda<Func<T, bool>>(body, param));
        }

        return source;
    }

    // Query-string instants arrive as Local (the binder converts them); values without offset are taken as UTC
    private static DateTime ToUtc(DateTime value) => value.Kind == DateTimeKind.Local
            ? value.ToUniversalTime()
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);

    // A null set means no branch restriction (Admin); ids are captured so EF sends them as a parameter
    public static IQueryable<T> WhereSucursalIn<T>(
        this IQueryable<T> source,
        Expression<Func<T, Guid>> selector,
        IReadOnlySet<Guid>? allowed)
    {
        if (allowed is null)
        {
            return source;
        }

        var ids = allowed.ToList();
        Expression<Func<List<Guid>>> idsAccessor = () => ids;
        var body = Expression.Call(idsAccessor.Body, typeof(List<Guid>).GetMethod(nameof(List<>.Contains))!, selector.Body);
        return source.Where(Expression.Lambda<Func<T, bool>>(body, selector.Parameters[0]));
    }
}
