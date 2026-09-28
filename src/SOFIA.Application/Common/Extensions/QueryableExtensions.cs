using System.Linq.Expressions;

namespace SOFIA.Application.Common.Extensions;

public static class QueryableExtensions
{
    // Normalizes input dates to UTC before comparing to ensure consistent behavior across timezones.
    public static IQueryable<T> WhereDateRange<T>(
        this IQueryable<T> source,
        Expression<Func<T, DateTime>> selector,
        DateTime? from,
        DateTime? to)
    {
        if (from.HasValue)
        {
            var fromUtc = DateTime.SpecifyKind(from.Value, DateTimeKind.Utc);
            var param = selector.Parameters[0];
            var body = Expression.GreaterThanOrEqual(selector.Body, Expression.Constant(fromUtc));
            source = source.Where(Expression.Lambda<Func<T, bool>>(body, param));
        }

        if (to.HasValue)
        {
            var toUtc = DateTime.SpecifyKind(to.Value, DateTimeKind.Utc);
            var param = selector.Parameters[0];
            var body = Expression.LessThanOrEqual(selector.Body, Expression.Constant(toUtc));
            source = source.Where(Expression.Lambda<Func<T, bool>>(body, param));
        }

        return source;
    }

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
