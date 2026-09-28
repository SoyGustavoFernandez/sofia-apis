using MediatR;
using SOFIA.Domain.Common;

namespace SOFIA.Application.SeriesFiscales.Queries.GetSerieFiscalById;

public record GetSerieFiscalByIdQuery(Guid Id) : IRequest<Result<SerieFiscalDto>>;
