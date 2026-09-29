using FluentAssertions;
using SOFIA.Application.Common.Excel;

namespace SOFIA.UnitTests.Common.Excel;

public class ImportNameLookupTests
{
    private readonly Guid _tableta = Guid.NewGuid();
    private readonly Guid _capsula = Guid.NewGuid();
    private readonly ImportNameLookup _lookup;

    public ImportNameLookupTests() =>
        _lookup = new ImportNameLookup([("Tableta", _tableta), ("TABLETA", Guid.NewGuid()), ("Capsula", _capsula)]);

    [Fact]
    public void TryGetUnique_ShouldResolveCaseInsensitively_WhenNameIsUnique()
    {
        var found = _lookup.TryGetUnique("CAPSULA", out var id);

        _ = found.Should().BeTrue();
        _ = id.Should().Be(_capsula);
        _ = _lookup.IsAmbiguous("capsula").Should().BeFalse();
    }

    [Fact]
    public void TryGetUnique_ShouldFail_WhenNameIsRepeated()
    {
        var found = _lookup.TryGetUnique("tableta", out var id);

        _ = found.Should().BeFalse();
        _ = id.Should().Be(Guid.Empty);
        _ = _lookup.Contains("tableta").Should().BeTrue();
        _ = _lookup.IsAmbiguous("tableta").Should().BeTrue();
    }

    [Theory]
    [InlineData("Jarabe")]
    [InlineData(null)]
    public void TryGetUnique_ShouldFail_WhenNameIsUnknown(string? name)
    {
        _ = _lookup.TryGetUnique(name, out _).Should().BeFalse();
        _ = _lookup.Contains(name).Should().BeFalse();
        _ = _lookup.IsAmbiguous(name).Should().BeFalse();
    }
}
