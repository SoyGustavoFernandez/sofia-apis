namespace SOFIA.IntegrationTests.Infrastructure;

/// <summary>Builds the "today" the SPA sends to the dashboard: the instant where the local day starts in Lima.</summary>
public static class DashboardTestClock
{
    private static readonly TimeSpan Lima = TimeSpan.FromHours(-5);

    public static DateTimeOffset HoyLima()
    {
        var ahora = DateTimeOffset.UtcNow.ToOffset(Lima);
        return new DateTimeOffset(ahora.Year, ahora.Month, ahora.Day, 0, 0, 0, Lima);
    }
}
