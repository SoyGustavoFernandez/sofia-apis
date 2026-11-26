using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Enums;

namespace SOFIA.Application.Onboarding.Commands.RegistrarPrimerProducto;

// Product, laboratory, batch and stock of the user's branch in a single step
public record RegistrarPrimerProductoCommand(
    string CodigoNacional,
    string NombreComercial,
    string LaboratorioNombre,
    Guid UnidadBaseId,
    CondicionVenta CondicionVenta,
    decimal PrecioVentaBase,
    string NumeroLote,
    DateTimeOffset FechaCaducidad,
    decimal Cantidad) : ICommand<Guid>;
