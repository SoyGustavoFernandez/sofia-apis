using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Recetas.Queries.GetRecetaById;

public class GetRecetaByIdQueryHandler(IApplicationDbContext context) : IRequestHandler<GetRecetaByIdQuery, Result<RecetaDto>>
{
    public async Task<Result<RecetaDto>> Handle(GetRecetaByIdQuery request, CancellationToken cancellationToken)
    {
        var dto = await (
            from r in context.Recetas.AsNoTracking().Where(x => x.Id == request.Id)
            join p in context.Pacientes on r.ClienteId equals p.Id into pGroup
            from p in pGroup.DefaultIfEmpty()
            join m in context.ProfesionalesSalud on r.MedicoId equals m.Id into mGroup
            from m in mGroup.DefaultIfEmpty()
            select new RecetaDto(
                r.Id,
                r.ClienteId,
                p != null ? p.NombreApellidos : string.Empty,
                r.MedicoId,
                m != null ? m.NombrePrescriptor : string.Empty,
                r.FechaExpedicion,
                r.RepeticionesMax,
                r.IndicacionesUso))
            .FirstOrDefaultAsync(cancellationToken);

        return dto == null
            ? Result.Failure<RecetaDto>(Error.NotFound("RecetaMedica.NotFound", "Receta médica not found."))
            : Result.Success(dto);
    }
}
