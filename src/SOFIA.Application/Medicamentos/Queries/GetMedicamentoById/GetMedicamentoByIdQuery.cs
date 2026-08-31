using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Inventarios.Queries.GetStockByMedicamento;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Medicamentos.Queries.GetMedicamentoById;

public record GetMedicamentoByIdQuery(Guid Id) : IRequest<Result<MedicamentoDto>>;
