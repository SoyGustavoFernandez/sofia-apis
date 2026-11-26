using Microsoft.Extensions.DependencyInjection;
using SOFIA.Application.Common.Interfaces;
using SOFIA.IntegrationTests.Infrastructure;
using SOFIA.Infrastructure.Services;

namespace SOFIA.IntegrationTests.Recetas;

public class PresidioRegistrationIntegrationTests(SofiaWebAppFactory factory) : IClassFixture<SofiaWebAppFactory>
{
    [Fact]
    public void PrivacyService_ShouldBeBuiltByTheApplicationContainer()
    {
        using var scope = factory.Services.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IPrivacyService>();

        _ = service.Should().BeOfType<PresidioPrivacyService>();
    }
}
