using FluentValidation;

namespace SOFIA.Application.Empleados.Queries.GetEmpleadosWithPagination;

public class GetEmpleadosWithPaginationQueryValidator : AbstractValidator<GetEmpleadosWithPaginationQuery>
{
    public GetEmpleadosWithPaginationQueryValidator() => RuleFor(x => x.SucursalId)
            .NotEqual(Guid.Empty)
            .When(x => x.SucursalId.HasValue);
}
