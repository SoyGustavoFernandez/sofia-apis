using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SOFIA.IntegrationTests.Infrastructure;

namespace SOFIA.IntegrationTests.Api;

public class LoggingConfigurationTests(SofiaWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Theory]
    [InlineData("Microsoft.EntityFrameworkCore.Database.Command")]
    [InlineData("Microsoft.AspNetCore.Hosting.Diagnostics")]
    [InlineData("System.Net.Http.HttpClient.Default.LogicalHandler")]
    public void LoggerFactory_ShouldNotLogInformation_WhenCategoryLeaksSqlOrQueryStrings(string category)
    {
        var logger = Factory.Services.GetRequiredService<ILoggerFactory>().CreateLogger(category);

        _ = logger.IsEnabled(LogLevel.Information).Should().BeFalse(because: "SQL text and request query strings (patient DNIs) must not reach the log files");
        _ = logger.IsEnabled(LogLevel.Warning).Should().BeTrue();
    }

    [Fact]
    public void LoggerFactory_ShouldLogInformation_WhenCategoryIsApplication()
    {
        var logger = Factory.Services.GetRequiredService<ILoggerFactory>().CreateLogger("SOFIA.Application");

        _ = logger.IsEnabled(LogLevel.Information).Should().BeTrue();
    }
}
