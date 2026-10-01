using FluentAssertions;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.Inventarios.Domain;

public class LoteInventarioTests
{
    private static readonly Guid ProductoId = Guid.NewGuid();
    private static readonly DateTimeOffset FutureDate = DateTimeOffset.UtcNow.AddYears(1);
    private static readonly DateTimeOffset PastDate = DateTimeOffset.UtcNow.AddYears(-1);
    private static readonly TimeSpan OffsetLima = TimeSpan.FromHours(-5);
    private static readonly DateTimeOffset CaducidadLima = new(DateTime.UtcNow.Year + 1, 12, 31, 0, 0, 0, OffsetLima);

    // Expiry stored like the SPA sends it (-05:00) for an expiry day that started the given hours ago.
    private static DateTimeOffset CaducidadIniciadaHace(double horas) => DateTimeOffset.UtcNow.ToOffset(OffsetLima).AddHours(-horas);

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

    [Theory]
    [InlineData(1)]
    [InlineData(12)]
    [InlineData(23)]
    public void Create_ShouldSucceed_WhenExpiryDayHasNotEnded(double horas)
    {
        var result = LoteInventario.Create(ProductoId, "LOT-001", null, CaducidadIniciadaHace(horas));

        _ = result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(24)]
    [InlineData(36)]
    public void Create_ShouldFail_WhenExpiryDayAlreadyEnded(double horas)
    {
        var result = LoteInventario.Create(ProductoId, "LOT-001", null, CaducidadIniciadaHace(horas));

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

    [Fact]
    public void Update_ShouldSucceed_WhenExpiryDayHasNotEnded()
    {
        var lote = LoteInventario.Create(ProductoId, "LOT-001", PastDate, FutureDate).Value!;
        var venceHoy = CaducidadIniciadaHace(12);

        var result = lote.Update("LOT-001", null, venceHoy);

        _ = result.IsSuccess.Should().BeTrue();
        _ = lote.FechaCaducidad.Should().Be(venceHoy);
    }

    [Fact]
    public void Update_ShouldFail_WhenExpiryDayAlreadyEnded()
    {
        var lote = LoteInventario.Create(ProductoId, "LOT-001", PastDate, FutureDate).Value!;

        var result = lote.Update("LOT-001", null, CaducidadIniciadaHace(36));

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("LoteInventario.FechaCaducidad");
        _ = lote.FechaCaducidad.Should().Be(FutureDate);
    }

    [Fact]
    public void EstaVencido_ShouldBeFalse_BeforeExpirationDate()
    {
        var lote = LoteInventario.Create(ProductoId, "LOT-001", null, FutureDate).Value!;

        _ = lote.EstaVencido(FutureDate.AddDays(-1)).Should().BeFalse();
    }

    [Fact]
    public void EstaVencido_ShouldBeFalse_AtTheStartOfTheExpiryDay()
    {
        var lote = LoteInventario.Create(ProductoId, "LOT-001", null, CaducidadLima).Value!;

        _ = lote.EstaVencido(CaducidadLima).Should().BeFalse();
    }

    [Fact]
    public void EstaVencido_ShouldBeFalse_AtTheLastSecondOfTheExpiryDay()
    {
        var lote = LoteInventario.Create(ProductoId, "LOT-001", null, CaducidadLima).Value!;

        _ = lote.EstaVencido(CaducidadLima.AddDays(1).AddSeconds(-1)).Should().BeFalse();
    }

    [Fact]
    public void EstaVencido_ShouldBeTrue_AtMidnightOfTheNextDay()
    {
        var lote = LoteInventario.Create(ProductoId, "LOT-001", null, CaducidadLima).Value!;

        _ = lote.EstaVencido(CaducidadLima.AddDays(1)).Should().BeTrue();
    }

    [Fact]
    public void EstaVencido_ShouldBeTrue_AfterTheExpiryDay()
    {
        var lote = LoteInventario.Create(ProductoId, "LOT-001", null, CaducidadLima).Value!;

        _ = lote.EstaVencido(CaducidadLima.AddDays(2)).Should().BeTrue();
    }

    [Theory]
    [InlineData(4, 59, 59, false)]
    [InlineData(5, 0, 0, true)]
    public void EstaVencido_ShouldUseTheStoredOffset_WhenNowIsUtc(int hora, int minuto, int segundo, bool vencido)
    {
        var lote = LoteInventario.Create(ProductoId, "LOT-001", null, CaducidadLima).Value!;
        var ahoraUtc = new DateTimeOffset(CaducidadLima.Year + 1, 1, 1, hora, minuto, segundo, TimeSpan.Zero);

        _ = lote.EstaVencido(ahoraUtc).Should().Be(vencido);
    }

    [Fact]
    public void UltimaCaducidadVencida_ShouldMatchEstaVencido_OnBothSidesOfTheBoundary()
    {
        var ahora = CaducidadLima.AddDays(1);
        var limite = LoteInventario.UltimaCaducidadVencida(ahora);

        _ = limite.Should().Be(CaducidadLima);
        _ = LoteInventario.CaducidadVencida(limite, ahora).Should().BeTrue();
        _ = LoteInventario.CaducidadVencida(limite.AddTicks(1), ahora).Should().BeFalse();
    }
}
