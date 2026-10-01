using System.Linq.Expressions;
using FluentAssertions;
using SOFIA.Application.Common.Extensions;

namespace SOFIA.UnitTests.Common.Extensions;

public class QueryableExtensionsTests
{
    private sealed record Item(DateTime Fecha);

    private static readonly DateTime Day = new(2026, 9, 30, 0, 0, 0, DateTimeKind.Utc);

    private static IQueryable<Item> Source() => new[]
    {
        new Item(Day.AddHours(-1)),
        new Item(Day),
        new Item(Day.AddHours(12)),
        new Item(Day.AddDays(1)),
        new Item(Day.AddDays(1).AddHours(1)),
    }.AsQueryable();

    private sealed class ConstantFinder : ExpressionVisitor
    {
        public bool HasDateTimeConstant { get; private set; }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            HasDateTimeConstant |= node.Type == typeof(DateTime);
            return base.VisitConstant(node);
        }
    }

    [Fact]
    public void WhereDateRange_ShouldReturnEverything_WhenNoBounds() => _ = Source().WhereDateRange(i => i.Fecha, null, null).Should().HaveCount(5);

    [Fact]
    public void WhereDateRange_ShouldIncludeBothBounds()
    {
        var result = Source().WhereDateRange(i => i.Fecha, Day, Day.AddDays(1)).ToList();

        _ = result.Select(i => i.Fecha).Should().Equal(Day, Day.AddHours(12), Day.AddDays(1));
    }

    [Fact]
    public void WhereDateRange_ShouldTreatUnspecifiedBoundsAsUtc()
    {
        var from = DateTime.SpecifyKind(Day, DateTimeKind.Unspecified);

        var result = Source().WhereDateRange(i => i.Fecha, from, null).ToList();

        _ = result.Should().HaveCount(4);
        _ = result.Min(i => i.Fecha).Should().Be(Day);
    }

    [Fact]
    public void WhereDateRange_ShouldConvertLocalBoundsToUtc_RegardlessOfServerTimeZone()
    {
        var fromLocal = Day.ToLocalTime();
        var toLocal = Day.AddDays(1).ToLocalTime();

        var result = Source().WhereDateRange(i => i.Fecha, fromLocal, toLocal).ToList();

        _ = fromLocal.Kind.Should().Be(DateTimeKind.Local);
        _ = result.Select(i => i.Fecha).Should().Equal(Day, Day.AddHours(12), Day.AddDays(1));
    }

    [Fact]
    public void WhereDateRange_ShouldSendBoundsAsParameters_NotInlineConstants()
    {
        var finder = new ConstantFinder();

        _ = finder.Visit(Source().WhereDateRange(i => i.Fecha, Day, Day.AddDays(1)).Expression);

        _ = finder.HasDateTimeConstant.Should().BeFalse();
    }
}
