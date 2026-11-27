using FluentAssertions;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.Plataforma;

public class AvisoEnviadoTests
{
    [Theory]
    [InlineData(8, null)]
    [InlineData(7, 7)]
    [InlineData(5, 7)]
    [InlineData(2, 7)]
    [InlineData(1, 1)]
    [InlineData(0, 1)]
    [InlineData(-1, null)]
    public void HitoPara_ShouldPickTheNoticeThatCorresponds(int diasRestantes, int? hito) =>
        _ = AvisoEnviado.HitoPara(diasRestantes).Should().Be(hito);

    [Fact]
    public void DiasRestantes_ShouldCountCalendarDaysAsSeenInPeru()
    {
        // 03:00 UTC on the 2nd is still the 1st in Peru
        var ahora = new DateTimeOffset(2026, 12, 2, 3, 0, 0, TimeSpan.Zero);
        var vencimiento = new DateTimeOffset(2026, 12, 8, 14, 0, 0, TimeSpan.Zero);

        _ = AvisoEnviado.DiasRestantes(vencimiento, ahora).Should().Be(7);
    }
}
