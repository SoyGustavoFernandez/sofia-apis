using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
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

    [Fact]
    public void Controller_Actions_Should_Declare_Authorization()
    {
        // Arrange
        var assembly = Assembly.Load(ApiNamespace);

        // Act
        var unprotected = assembly.GetTypes()
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract && !HasAuthMetadata(t))
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => m.GetCustomAttributes<HttpMethodAttribute>().Any() && !HasAuthMetadata(m))
                .Select(m => $"{t.Name}.{m.Name}"))
            .ToList();

        Assert.True(unprotected.Count == 0, $"Actions without [HasPermission], [Authorize] or [AllowAnonymous]: {string.Join(", ", unprotected)}");
    }

    private static bool HasAuthMetadata(MemberInfo member) =>
        member.GetCustomAttributes(inherit: true).Any(a => a is IAuthorizeData or IAllowAnonymous);
}
