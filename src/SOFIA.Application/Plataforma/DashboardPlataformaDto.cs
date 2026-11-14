namespace SOFIA.Application.Plataforma;

/// <summary>Subscription figures across every company for the platform operator; Vigentes is EnPrueba plus ConSuscripcion.</summary>
public record DashboardPlataformaDto(
    int Total,
    int Vigentes,
    int EnPrueba,
    int ConSuscripcion,
    int PorVencer,
    int Vencidas,
    int Suspendidas,
    int Canceladas,
    int RegistradasEsteMes,
    int DiasPorVencer,
    IReadOnlyList<EmpresaPorVencerDto> ProximasAVencer
);
