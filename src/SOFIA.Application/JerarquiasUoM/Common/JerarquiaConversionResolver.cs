namespace SOFIA.Application.JerarquiasUoM.Common;

/// <summary>
/// Walks a product's UoM conversion graph (its JerarquiaUoM edges) to resolve how many
/// "hacia" units equal one "desde" unit, even across multiple hops (e.g. Caja -> Blister -> Unidad).
/// Pure/DB-free so it can be reused by both the JerarquiaUoM consistency check and the
/// PresentacionVenta resolver, and unit-tested without mocking a DbContext.
/// </summary>
public static class JerarquiaConversionResolver
{
    public readonly record struct Edge(Guid UnidadMayorId, Guid UnidadMenorId, decimal Multiplicador);

    /// <returns>How many units of <paramref name="hacia"/> equal one unit of <paramref name="desde"/>, or null if no path connects them.</returns>
    public static decimal? Resolve(IEnumerable<Edge> edges, Guid desde, Guid hacia)
    {
        if (desde == hacia)
        {
            return 1m;
        }

        var adjacency = new Dictionary<Guid, List<(Guid Vecino, decimal Factor)>>();
        void AddEdge(Guid from, Guid to, decimal factor)
        {
            if (!adjacency.TryGetValue(from, out var vecinos))
            {
                vecinos = [];
                adjacency[from] = vecinos;
            }
            vecinos.Add((to, factor));
        }

        foreach (var edge in edges)
        {
            // Mayor -> Menor multiplies (1 Caja -> 4 Blister); Menor -> Mayor divides.
            AddEdge(edge.UnidadMayorId, edge.UnidadMenorId, edge.Multiplicador);
            AddEdge(edge.UnidadMenorId, edge.UnidadMayorId, 1m / edge.Multiplicador);
        }

        var visited = new HashSet<Guid> { desde };
        var queue = new Queue<(Guid Nodo, decimal FactorAcumulado)>();
        queue.Enqueue((desde, 1m));

        while (queue.Count > 0)
        {
            var (nodo, factor) = queue.Dequeue();

            if (!adjacency.TryGetValue(nodo, out var vecinos))
            {
                continue;
            }

            foreach (var (vecino, edgeFactor) in vecinos)
            {
                if (!visited.Add(vecino))
                {
                    continue;
                }

                var factorAcumulado = factor * edgeFactor;
                if (vecino == hacia)
                {
                    return factorAcumulado;
                }

                queue.Enqueue((vecino, factorAcumulado));
            }
        }

        return null;
    }
}
