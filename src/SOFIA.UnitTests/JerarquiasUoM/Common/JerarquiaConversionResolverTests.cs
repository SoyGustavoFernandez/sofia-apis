using FluentAssertions;
using SOFIA.Application.JerarquiasUoM.Common;

namespace SOFIA.UnitTests.JerarquiasUoM.Common;

public class JerarquiaConversionResolverTests
{
    private static readonly Guid Caja = Guid.NewGuid();
    private static readonly Guid Blister = Guid.NewGuid();
    private static readonly Guid Unidad = Guid.NewGuid();
    private static readonly Guid Miligramo = Guid.NewGuid();

    [Fact]
    public void Resolve_ShouldReturnMultiplicador_WhenDirectEdgeExists()
    {
        JerarquiaConversionResolver.Edge[] edges = [new(Caja, Unidad, 40m)];

        var result = JerarquiaConversionResolver.Resolve(edges, Caja, Unidad);

        _ = result.Should().Be(40m);
    }

    [Fact]
    public void Resolve_ShouldMultiplyAlongTheChain_WhenPathHasMultipleHops()
    {
        // 1 Caja = 4 Blister, 1 Blister = 10 Unidad -> 1 Caja = 40 Unidad
        JerarquiaConversionResolver.Edge[] edges =
        [
            new(Caja, Blister, 4m),
            new(Blister, Unidad, 10m),
        ];

        var result = JerarquiaConversionResolver.Resolve(edges, Caja, Unidad);

        _ = result.Should().Be(40m);
    }

    [Fact]
    public void Resolve_ShouldDivide_WhenTraversingFromSmallerToLargerUnit()
    {
        JerarquiaConversionResolver.Edge[] edges = [new(Caja, Unidad, 40m)];

        var result = JerarquiaConversionResolver.Resolve(edges, Unidad, Caja);

        _ = result.Should().Be(1m / 40m);
    }

    [Fact]
    public void Resolve_ShouldReturnOne_WhenSameUnitOnBothSides()
    {
        var result = JerarquiaConversionResolver.Resolve([], Caja, Caja);

        _ = result.Should().Be(1m);
    }

    [Fact]
    public void Resolve_ShouldReturnNull_WhenNoPathConnectsTheUnits()
    {
        JerarquiaConversionResolver.Edge[] edges = [new(Caja, Blister, 4m)];

        var result = JerarquiaConversionResolver.Resolve(edges, Caja, Miligramo);

        _ = result.Should().BeNull();
    }

    [Fact]
    public void Resolve_ShouldFollowShortestPath_WhenARedundantConsistentEdgeAlsoExists()
    {
        JerarquiaConversionResolver.Edge[] edges =
        [
            new(Caja, Blister, 4m),
            new(Blister, Unidad, 10m),
            new(Caja, Unidad, 40m),
        ];

        var result = JerarquiaConversionResolver.Resolve(edges, Caja, Unidad);

        _ = result.Should().Be(40m);
    }
}
