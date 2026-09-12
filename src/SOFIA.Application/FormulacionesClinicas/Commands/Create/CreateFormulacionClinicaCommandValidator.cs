using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.FormulacionesClinicas.Commands.Create;

public class CreateFormulacionClinicaCommandValidator : AbstractValidator<CreateFormulacionClinicaCommand>
{
    public CreateFormulacionClinicaCommandValidator()
    {
        _ = RuleFor(v => v.ProductoId).NotEmpty();
        _ = RuleFor(v => v.IngredienteId).NotEmpty();
        _ = RuleFor(v => v.ConcentracionDosis).GreaterThan(0);
        _ = RuleFor(v => v.UnidadMedidaId).NotEmpty();
        _ = RuleFor(v => v.CodigoTeOrange).MaximumLength(5);
    }
}
