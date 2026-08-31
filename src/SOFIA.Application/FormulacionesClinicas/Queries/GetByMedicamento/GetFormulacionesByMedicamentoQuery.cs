using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.FormulacionesClinicas.Queries.GetByMedicamento;

public record GetFormulacionesByMedicamentoQuery(Guid ProductoId) : IRequest<Result<List<FormulacionClinicaDto>>>;
