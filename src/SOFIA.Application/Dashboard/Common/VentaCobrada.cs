namespace SOFIA.Application.Dashboard.Common;

// Amount the customer had to pay for one collected sale: gross total minus insurance cover
public record VentaCobrada(Guid VentaId, decimal MontoAPagar);
