namespace SOFIA.Application.Dashboard.Common;

// Half-open 24h window of one local day: [DesdeUtc, HastaUtc)
public record VentanaDia(DateTimeOffset Desde, DateTime DesdeUtc, DateTime HastaUtc);
