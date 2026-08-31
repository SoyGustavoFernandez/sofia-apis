using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.POS.Commands.CerrarCaja;

public class CerrarCajaCommandValidator : AbstractValidator<CerrarCajaCommand>
{
    public CerrarCajaCommandValidator()
    {
        _ = RuleFor(v => v.SesionId).NotEmpty();
        _ = RuleFor(v => v.MontoCierre).GreaterThanOrEqualTo(0);
    }
}
