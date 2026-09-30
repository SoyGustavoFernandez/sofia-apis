using FluentAssertions;
using SOFIA.Application.Security;

namespace SOFIA.UnitTests.Security;

public class PasswordRecoveryEmailTests
{
    private const string Link = "https://app.sofia.test/auth/reset-password#token=abc-123&usuario=ana";

    [Fact]
    public void Build_ShouldFillBothPartsFromTheEmbeddedTemplates()
    {
        var message = PasswordRecoveryEmail.Build("ana@farmacia.pe", "Ana", "ana", Link);

        _ = message.To.Should().Be("ana@farmacia.pe");
        _ = message.Subject.Should().Be(PasswordRecoveryEmail.Subject);
        _ = message.TextBody.Should().Contain("Hola, Ana:").And.Contain(Link).And.Contain("1 hora").And.NotContain("{{");
        _ = message.HtmlBody.Should().Contain("<!DOCTYPE html>").And.Contain("href=\"https://app.sofia.test/auth/reset-password#token=abc-123&amp;usuario=ana\"").And.NotContain("{{");
    }

    [Fact]
    public void Build_ShouldHtmlEncodeTheEmployeeName_InTheHtmlPart()
    {
        var message = PasswordRecoveryEmail.Build("ana@farmacia.pe", "<script>x</script>", "ana", Link);

        _ = message.HtmlBody.Should().Contain("&lt;script&gt;x&lt;/script&gt;").And.NotContain("<script>");
    }
}
