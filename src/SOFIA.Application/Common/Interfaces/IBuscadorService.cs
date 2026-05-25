using SOFIA.Domain.Entities;

namespace SOFIA.Application.Common.Interfaces;

public interface IBuscadorService
{
    Task<List<DIGEMIDCatalogoProducto>> BuscarEnDigemidAsync(string termino, int top = 5, CancellationToken cancellationToken = default);
    Task<Medicamento?> BuscarMejorCoincidenciaAsync(string termino, CancellationToken cancellationToken = default);
}
