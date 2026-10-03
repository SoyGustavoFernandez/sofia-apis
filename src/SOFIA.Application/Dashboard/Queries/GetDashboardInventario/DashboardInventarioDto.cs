namespace SOFIA.Application.Dashboard.Queries.GetDashboardInventario;

public record DashboardInventarioDto(
    int LotesVencidosConStock,
    int LotesVencen30,
    int LotesVencen60,
    int LotesVencen90,
    int ProductosSinStock,
    int ProductosStockBajo,
    decimal UmbralStockBajo);
