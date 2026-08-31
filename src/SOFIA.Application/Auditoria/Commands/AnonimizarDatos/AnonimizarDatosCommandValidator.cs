using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Auditoria.Commands.AnonimizarDatos;

public class AnonimizarDatosCommandValidator : AbstractValidator<AnonimizarDatosCommand>
{
    public AnonimizarDatosCommandValidator()
    {
        _ = RuleFor(v => v.Tabla).NotEmpty();
        _ = RuleFor(v => v.RegistroId).NotEmpty();
    }
}
