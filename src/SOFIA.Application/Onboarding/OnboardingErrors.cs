using SOFIA.Domain.Common;

namespace SOFIA.Application.Onboarding;

public static class OnboardingErrors
{
    public static readonly Error SoloAdministrador = Error.Forbidden("Onboarding.SoloAdministrador", "Only the company administrator can run the first steps.");
}
