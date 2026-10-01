namespace SOFIA.Domain.Enums;

// Stored as text in Estado_Aceptacion (VARCHAR(20)); Aceptado and Rechazado are reserved for the future OSE/PSE integration
public static class EstadoComprobanteSunat
{
    public const string NoEnviado = "NoEnviado";
    public const string Aceptado = "Aceptado";
    public const string Rechazado = "Rechazado";
    public const string Anulado = "Anulado";
}
