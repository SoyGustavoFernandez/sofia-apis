using FluentAssertions;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.Inventarios.Domain;

public class LoteInventarioTests
{
    private static readonly Guid ProductoId = Guid.NewGuid();
    private static readonly DateTimeOffset FutureDate = DateTimeOffset.UtcNow.AddYears(1);
    private static readonly DateTimeOffset PastDate = DateTimeOffset.UtcNow.AddYears(-1);

    [Fact]
    public void Create_ShouldSucceed_WhenDataIsValid()
    {
        var result = LoteInventario.Create(ProductoId, "LOT-001", PastDate, FutureDate);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value!.ProductoId.Should().Be(ProductoId);
        _ = result.Value.NumeroLoteMfr.Should().Be("LOT-001");
        _ = result.Value.FechaCaducidad.Should().Be(FutureDate);
    }

    [Fact]
    public void Create_ShouldSucceed_WhenManufactureDateIsNull()
    {
        var result = LoteInventario.Create(ProductoId, "LOT-001", null, FutureDate);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value!.FechaFabricacion.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldFail_WhenProductoIdIsEmpty()
    {
        var result = LoteInventario.Create(Guid.Empty, "LOT-001", null, FutureDate);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("LoteInventario.ProductoId");
    }

    [Fact]
    public void Create_ShouldFail_WhenBatchNumberIsEmpty()
    {
        var result = LoteInventario.Create(ProductoId, "   ", null, FutureDate);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("LoteInventario.NumeroLoteMfr");
    }

    [Fact]
    public void Create_ShouldFail_WhenBatchNumberExceeds100Characters()
    {
        var result = LoteInventario.Create(ProductoId, new string('x', 101), null, FutureDate);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("LoteInventario.NumeroLoteMfr");
    }

    [Fact]
    public void Create_ShouldFail_WhenExpirationDateIsNotInTheFuture()
    {
        var result = LoteInventario.Create(ProductoId, "LOT-001", null, PastDate);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("LoteInventario.FechaCaducidad");
    }

    [Fact]
    public void Create_ShouldFail_WhenManufactureDateIsInTheFuture()
    {
        var result = LoteInventario.Create(ProductoId, "LOT-001", DateTimeOffset.UtcNow.AddDays(5), FutureDate);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("LoteInventario.FechaFabricacion");
    }

    [Fact]
    public void Update_ShouldMutateState_WhenDataIsValid()
    {
        var lote = LoteInventario.Create(ProductoId, "LOT-001", PastDate, FutureDate).Value!;
        var newExpiration = FutureDate.AddMonths(3);

        var result = lote.Update("LOT-002", null, newExpiration);

        _ = result.IsSuccess.Should().BeTrue();
        _ = lote.NumeroLoteMfr.Should().Be("LOT-002");
        _ = lote.FechaFabricacion.Should().BeNull();
        _ = lote.FechaCaducidad.Should().Be(newExpiration);
    }

    [Fact]
    public void Update_ShouldFail_WhenExpirationDateIsNotInTheFuture()
    {
        var lote = LoteInventario.Create(ProductoId, "LOT-001", PastDate, FutureDate).Value!;

        var result = lote.Update("LOT-001", null, PastDate);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("LoteInventario.FechaCaducidad");
    }
}
