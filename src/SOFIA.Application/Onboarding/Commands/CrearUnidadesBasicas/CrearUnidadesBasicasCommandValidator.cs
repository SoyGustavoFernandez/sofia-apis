using FluentValidation;

namespace SOFIA.Application.Onboarding.Commands.CrearUnidadesBasicas;

// The command carries no input, so there is nothing to validate
public class CrearUnidadesBasicasCommandValidator : AbstractValidator<CrearUnidadesBasicasCommand>;
