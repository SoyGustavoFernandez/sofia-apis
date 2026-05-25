using NetArchTest.Rules;
using System.Reflection;

namespace SOFIA.ArchitectureTests;

public class ArchitectureTests
{
    private const string DomainNamespace = "SOFIA.Domain";
    private const string ApplicationNamespace = "SOFIA.Application";
    private const string InfrastructureNamespace = "SOFIA.Infrastructure";
    private const string ApiNamespace = "SOFIA.API";

    [Fact]
    public void Domain_Should_Not_Have_Dependency_On_Other_Projects()
    {
        // Arrange
        var assembly = Assembly.Load(DomainNamespace);

        var otherProjects = new[]
        {
            ApplicationNamespace,
            InfrastructureNamespace,
            ApiNamespace
        };

        // Act
        var result = Types.InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOnAny(otherProjects)
            .GetResult();

        Assert.True(result.IsSuccessful, $"Domain layer should not depend on other layers.");
    }

    [Fact]
    public void Application_Should_Only_Depend_On_Domain()
    {
        // Arrange
        var assembly = Assembly.Load(ApplicationNamespace);

        var prohibitedProjects = new[]
        {
            InfrastructureNamespace,
            ApiNamespace
        };

        // Act
        var result = Types.InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOnAny(prohibitedProjects)
            .GetResult();

        Assert.True(result.IsSuccessful, "Application layer should only depend on Domain.");
    }
}
