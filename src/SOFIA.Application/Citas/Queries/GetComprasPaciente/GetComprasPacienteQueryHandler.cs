using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Enums;

namespace SOFIA.Application.Citas.Queries.GetComprasPaciente;

public class GetComprasPacienteQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetComprasPacienteQuery, Result<List<CompraPacienteDto>>>
{
    private const int Maximo = 50;

    public async Task<Result<List<CompraPacienteDto>>> Handle(GetComprasPacienteQuery request, CancellationToken cancellationToken)
    {
        var compras = await (
            from d in context.DetallesVenta
            join v in context.Ventas on d.VentaId equals v.Id
            join l in context.LotesInventario on d.LoteId equals l.Id
            join m in context.Medicamentos on l.ProductoId equals m.Id
            where v.ClienteId == request.PacienteId && v.Estado == EstadoVenta.Completada
            orderby v.FechaHoraUtc descending
            select new CompraPacienteDto(v.Id, v.FechaHoraUtc, l.Id, m.NombreComercial, l.NumeroLoteMfr))
            .Take(Maximo)
            .ToListAsync(cancellationToken);

        return Result.Success(compras);
    }
}
