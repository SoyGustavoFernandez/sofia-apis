using SOFIA.Infrastructure.Authentication;

namespace SOFIA.IntegrationTests.Security;

public class JwtOptionsTests
{
    private static JwtOptions Options(string secretKey = "0123456789abcdef0123456789abcdef", string issuer = "SOFIA.API", string audience = "SOFIA.Clients") =>
        new() { SecretKey = secretKey, Issuer = issuer, Audience = audience, ExpiryMinutes = 15 };

    [Fact]
    public void EnsureValid_ShouldPass_WhenKeyHas32BytesAndIssuerAudienceAreSet()
    {
        var act = () => Options().EnsureValid();

        _ = act.Should().NotThrow();
    }

    [Theory]
    [InlineData("")]
    [InlineData("short-secret-key")]
    [InlineData("0123456789abcdef0123456789abcde")]
    public void EnsureValid_ShouldThrow_WhenSecretKeyIsMissingOrShorterThan32Bytes(string secretKey)
    {
        var act = () => Options(secretKey: secretKey).EnsureValid();

        _ = act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData("", "SOFIA.Clients")]
    [InlineData("SOFIA.API", " ")]
    public void EnsureValid_ShouldThrow_WhenIssuerOrAudienceIsEmpty(string issuer, string audience)
    {
        var act = () => Options(issuer: issuer, audience: audience).EnsureValid();

        _ = act.Should().Throw<InvalidOperationException>();
    }
}
