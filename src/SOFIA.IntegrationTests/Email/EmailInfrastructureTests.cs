using MailKit.Security;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SOFIA.Application.Common.Models;
using SOFIA.Infrastructure.Email;

namespace SOFIA.IntegrationTests.Email;

public class EmailInfrastructureTests
{
    private static readonly EmailMessage Message = new("ana@farmacia.pe", "Asunto", "<p>Hola</p>", "Hola");

    private static EmailOptions Smtp(string host = "localhost", int port = 1025, bool useSsl = false, string? user = null, string? password = null) => new()
    {
        Provider = "Smtp",
        FromAddress = "no-reply@sofia.test",
        FromDisplayName = "SOFIA",
        Smtp = new SmtpSettings { Host = host, Port = port, UseSsl = useSsl, UserName = user, Password = password },
    };

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("None")]
    public void EnsureValid_ShouldAllowADisabledProvider_OnlyWhenAllowed(string? provider)
    {
        var options = new EmailOptions { Provider = provider! };

        _ = options.Invoking(o => o.EnsureValid(allowDisabled: true)).Should().NotThrow();
        _ = options.Invoking(o => o.EnsureValid(allowDisabled: false)).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void EnsureValid_ShouldPass_ForACompleteSmtpConfiguration() =>
        _ = Smtp().Invoking(o => o.EnsureValid(allowDisabled: false)).Should().NotThrow();

    [Fact]
    public void EnsureValid_ShouldPass_ForACompleteAzureCommunicationConfiguration() =>
        _ = new EmailOptions
        {
            Provider = "AzureCommunication",
            FromAddress = "DoNotReply@example.azurecomm.net",
            AzureCommunication = new AzureCommunicationSettings { ConnectionString = "endpoint=https://example.communication.azure.com/;accesskey=dGVzdA==" },
        }.Invoking(o => o.EnsureValid(allowDisabled: false)).Should().NotThrow();

    public static TheoryData<EmailOptions> IncompleteConfigurations() =>
    [
        new() { Provider = "Smtp", FromAddress = "", Smtp = new SmtpSettings { Host = "localhost", Port = 1025 } },
        new() { Provider = "Smtp", FromAddress = "no-reply@sofia.test", Smtp = new SmtpSettings { Host = "", Port = 1025 } },
        new() { Provider = "Smtp", FromAddress = "no-reply@sofia.test", Smtp = new SmtpSettings { Host = "localhost", Port = 0 } },
        new() { Provider = "Smtp", FromAddress = "no-reply@sofia.test", Smtp = new SmtpSettings { Host = "localhost", Port = 587, UserName = "user" } },
        new() { Provider = "AzureCommunication", FromAddress = "DoNotReply@example.azurecomm.net" },
        new() { Provider = "SendGrid", FromAddress = "no-reply@sofia.test" },
    ];

    [Theory]
    [MemberData(nameof(IncompleteConfigurations))]
    public void EnsureValid_ShouldThrow_WhenTheSelectedProviderIsIncompleteOrUnknown(EmailOptions options) => _ = options.Invoking(o => o.EnsureValid(allowDisabled: true)).Should().Throw<InvalidOperationException>();

    [Theory]
    [InlineData("https://sofia.azurestaticapps.net", false)]
    [InlineData("http://localhost:4200", true)]
    public void AppOptions_EnsureValid_ShouldAcceptTheSpaUrl(string url, bool allowHttp) =>
        _ = new AppOptions { FrontendBaseUrl = url }.Invoking(o => o.EnsureValid(allowHttp)).Should().NotThrow();

    [Theory]
    [InlineData("")]
    [InlineData("sofia.test")]
    [InlineData("http://sofia.test")]
    [InlineData("ftp://sofia.test")]
    public void AppOptions_EnsureValid_ShouldThrow_WhenMissingRelativeOrNotHttpsOutsideDevelopment(string url) =>
        _ = new AppOptions { FrontendBaseUrl = url }.Invoking(o => o.EnsureValid(allowHttp: false)).Should().Throw<InvalidOperationException>();

    [Fact]
    public void FrontendLinks_PasswordReset_ShouldPutTokenAndUsernameInTheFragment()
    {
        var links = new FrontendLinks(Options.Create(new AppOptions { FrontendBaseUrl = "https://app.sofia.test/" }));

        var link = links.PasswordReset("a+b/c=", "ana pérez");

        _ = link.Should().Be("https://app.sofia.test/auth/reset-password#token=a%2Bb%2Fc%3D&usuario=ana%20p%C3%A9rez");
        _ = new Uri(link).Query.Should().BeEmpty(because: "nothing secret may travel in the query string");
    }

    [Fact]
    public void SmtpEmailSender_CreateMimeMessage_ShouldMapSenderRecipientSubjectAndBothBodies()
    {
        var mime = SmtpEmailSender.CreateMimeMessage(Smtp(), Message);

        var from = mime.From.Mailboxes.Should().ContainSingle().Subject;
        _ = from.Name.Should().Be("SOFIA");
        _ = from.Address.Should().Be("no-reply@sofia.test");
        _ = mime.To.Mailboxes.Should().ContainSingle().Which.Address.Should().Be("ana@farmacia.pe");
        _ = mime.Subject.Should().Be("Asunto");
        _ = mime.HtmlBody.Should().Be("<p>Hola</p>");
        _ = mime.TextBody.Should().Be("Hola");
        _ = mime.MessageId.Should().EndWith("@sofia.test");
    }

    [Theory]
    [InlineData(false, 1025, SecureSocketOptions.None)]
    [InlineData(true, 587, SecureSocketOptions.StartTls)]
    [InlineData(true, 465, SecureSocketOptions.SslOnConnect)]
    public void SmtpEmailSender_SocketOptions_ShouldFollowUseSslAndPort(bool useSsl, int port, SecureSocketOptions expected) =>
        _ = SmtpEmailSender.SocketOptions(new SmtpSettings { Host = "smtp.test", Port = port, UseSsl = useSsl }).Should().Be(expected);

    [Fact]
    public async Task SmtpEmailSender_SendAsync_ShouldReturnFailureInsteadOfThrowing_WhenTheServerIsUnreachable()
    {
        var sender = new SmtpEmailSender(Options.Create(Smtp(host: "127.0.0.1", port: 9)), NullLogger<SmtpEmailSender>.Instance);

        var result = await sender.SendAsync(Message);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Email.SendFailed");
    }

    [Fact]
    public void AzureCommunicationEmailSender_CreateMessage_ShouldMapSenderRecipientSubjectAndBothBodies()
    {
        var options = new EmailOptions { Provider = "AzureCommunication", FromAddress = "DoNotReply@example.azurecomm.net" };

        var message = AzureCommunicationEmailSender.CreateMessage(options, Message);

        _ = message.SenderAddress.Should().Be("DoNotReply@example.azurecomm.net");
        _ = message.Recipients.To.Should().ContainSingle().Which.Address.Should().Be("ana@farmacia.pe");
        _ = message.Content.Subject.Should().Be("Asunto");
        _ = message.Content.Html.Should().Be("<p>Hola</p>");
        _ = message.Content.PlainText.Should().Be("Hola");
    }
}
