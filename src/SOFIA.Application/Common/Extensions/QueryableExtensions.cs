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
}
