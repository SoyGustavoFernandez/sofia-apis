using FluentAssertions;
using MockQueryable;
using SOFIA.Application.Common.Models;

namespace SOFIA.UnitTests.Common.Models;

public class PaginatedListTests
{
    private sealed record Item(int Value);

    private static IQueryable<Item> Source(int count) => Enumerable.Range(1, count).Select(i => new Item(i)).ToList().BuildMock();

    [Fact]
    public async Task CreateAsync_ShouldCapPageSize_WhenPageSizeExceedsMaximum()
    {
        var result = await PaginatedList<Item>.CreateAsync(Source(PaginationLimits.MaxPageSize + 5), 1, int.MaxValue);

        _ = result.Items.Should().HaveCount(PaginationLimits.MaxPageSize);
        _ = result.TotalCount.Should().Be(PaginationLimits.MaxPageSize + 5);
        _ = result.TotalPages.Should().Be(2);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public async Task CreateAsync_ShouldReturnFirstPage_WhenPageNumberIsNotPositive(int pageNumber)
    {
        var result = await PaginatedList<Item>.CreateAsync(Source(30), pageNumber, 10);

        _ = result.PageNumber.Should().Be(1);
        _ = result.Items.Select(i => i.Value).Should().Equal(Enumerable.Range(1, 10));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task CreateAsync_ShouldUseMinimumPageSize_WhenPageSizeIsNotPositive(int pageSize)
    {
        var result = await PaginatedList<Item>.CreateAsync(Source(3), 1, pageSize);

        _ = result.Items.Select(i => i.Value).Should().Equal(1);
        _ = result.TotalPages.Should().Be(3);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnEmptyPage_WhenOffsetWouldOverflow()
    {
        var result = await PaginatedList<Item>.CreateAsync(Source(30), int.MaxValue, 100);

        _ = result.Items.Should().BeEmpty();
        _ = result.TotalCount.Should().Be(30);
    }

    [Fact]
    public void Constructor_ShouldNormalizePaging_WhenBuiltFromMappedItems()
    {
        var result = new PaginatedList<Item>([new(1), new(2)], count: 20, pageNumber: 0, pageSize: 0);

        _ = result.PageNumber.Should().Be(1);
        _ = result.TotalPages.Should().Be(20);
    }
}
