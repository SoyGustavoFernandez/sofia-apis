using SOFIA.Application.Common.Interfaces;

namespace SOFIA.Application.Onboarding.Commands.CrearUnidadesBasicas;

// Returns how many units were created: zero when the company already has its own
public record CrearUnidadesBasicasCommand : ICommand<int>;
