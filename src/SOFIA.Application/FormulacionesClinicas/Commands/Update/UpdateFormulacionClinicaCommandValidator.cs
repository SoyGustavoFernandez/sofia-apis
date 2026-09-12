using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.FormulacionesClinicas.Commands.Update;

public class UpdateFormulacionClinicaCommandValidator : AbstractValidator<UpdateFormulacionClinicaCommand>
{
    public UpdateFormulacionClinicaCommandValidator()
    {
        _ = RuleFor(v => v.Id).NotEmpty();
        _ = RuleFor(v => v.IngredienteId).NotEmpty();
        _ = RuleFor(v => v.ConcentracionDosis).GreaterThan(0);
        _ = RuleFor(v => v.UnidadMedidaId).NotEmpty();
        _ = RuleFor(v => v.CodigoTeOrange).MaximumLength(5);
    }
}
