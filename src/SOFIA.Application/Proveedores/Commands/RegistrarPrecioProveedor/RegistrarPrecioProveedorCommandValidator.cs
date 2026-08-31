using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Proveedores.Commands.RegistrarPrecioProveedor;

public class RegistrarPrecioProveedorCommandValidator : AbstractValidator<RegistrarPrecioProveedorCommand>
{
    public RegistrarPrecioProveedorCommandValidator()
    {
        _ = RuleFor(v => v.ProveedorId).NotEmpty();
        _ = RuleFor(v => v.MedicamentoId).NotEmpty();
        _ = RuleFor(v => v.PrecioCompra).GreaterThan(0);
        _ = RuleFor(v => v.DescuentoPorcentaje).InclusiveBetween(0, 100);
    }
}
