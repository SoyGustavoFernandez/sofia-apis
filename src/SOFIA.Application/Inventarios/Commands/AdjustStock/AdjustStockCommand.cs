using SOFIA.Application.Common.Interfaces;

namespace SOFIA.Application.Inventarios.Commands.AdjustStock;

public record AdjustStockCommand(Guid Id, decimal NuevaCantidad) : ICommand;
