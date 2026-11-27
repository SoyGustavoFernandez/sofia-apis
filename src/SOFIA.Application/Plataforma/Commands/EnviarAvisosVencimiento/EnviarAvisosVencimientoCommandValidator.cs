using FluentValidation;

namespace SOFIA.Application.Plataforma.Commands.EnviarAvisosVencimiento;

// The command carries no input, so there is nothing to validate
public class EnviarAvisosVencimientoCommandValidator : AbstractValidator<EnviarAvisosVencimientoCommand>;
