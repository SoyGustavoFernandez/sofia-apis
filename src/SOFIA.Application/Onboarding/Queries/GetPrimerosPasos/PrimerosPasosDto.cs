namespace SOFIA.Application.Onboarding.Queries.GetPrimerosPasos;

// Progress towards the first sale in the user's branch; Mostrar is false once there is nothing to guide
public record PrimerosPasosDto(bool Mostrar, Guid? SucursalId, bool SerieBoleta, bool ProductoConStock, bool CajaAbierta, bool EquipoAgregado)
{
    public static readonly PrimerosPasosDto Oculto = new(false, null, false, false, false, false);
}
