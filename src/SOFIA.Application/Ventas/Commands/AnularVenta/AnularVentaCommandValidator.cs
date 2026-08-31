using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Ventas.Commands.AnularVenta;

public class AnularVentaCommandValidator : AbstractValidator<AnularVentaCommand>
{
    public AnularVentaCommandValidator()
    {
        _ = RuleFor(x => x.VentaId).NotEmpty();
        _ = RuleFor(x => x.Motivo).NotEmpty().MaximumLength(255);
    }
}
