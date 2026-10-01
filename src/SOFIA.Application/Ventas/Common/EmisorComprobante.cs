namespace SOFIA.Application.Ventas.Common;

// Legal issuer data printed on the receipt: company name and RUC plus the issuing branch's address
public record EmisorComprobante(string RazonSocial, string? Ruc, string Direccion);
