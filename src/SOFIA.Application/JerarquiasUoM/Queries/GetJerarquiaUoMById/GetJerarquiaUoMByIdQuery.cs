using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.JerarquiasUoM.Queries.GetJerarquiaUoMById;

public record GetJerarquiaUoMByIdQuery(Guid Id) : IRequest<Result<JerarquiaUoMDto>>;
