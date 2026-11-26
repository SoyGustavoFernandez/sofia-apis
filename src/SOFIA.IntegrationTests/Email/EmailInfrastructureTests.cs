using System.Net;
using System.Net.Sockets;
using MailKit.Security;
using Microsoft.Extensions.Logging;
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
    public async Task SmtpEmailSender_SendAsync_ShouldLogTheFailingStepAndServerReplyWithoutAddresses_WhenTheServerRejects()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var server = RejectSenderAsync(listener);
        var logger = new ListLogger();
        var sender = new SmtpEmailSender(Options.Create(Smtp(host: "127.0.0.1", port: ((IPEndPoint)listener.LocalEndpoint).Port)), logger);

        var result = await sender.SendAsync(Message);
        await server;

        _ = result.IsFailure.Should().BeTrue();
        var line = logger.Lines.Should().ContainSingle().Subject;
        _ = line.Should().Contain("Step: Send").And.Contain("[550]").And.Contain("<email> rejected");
        _ = line.Should().NotContain("ana@farmacia.pe").And.NotContain("no-reply@sofia.test.");
    }

    [Fact]
    public void SmtpEmailSender_Describe_ShouldIncludeInnerCausesAndMaskAddresses()
    {
        var exception = new InvalidOperationException("Failed for <ana@farmacia.pe>", new IOException("Connection reset by luis.rojas+1@farmacia.com.pe"));

        var detail = SmtpEmailSender.Describe(exception);

        _ = detail.Should().Be("InvalidOperationException: Failed for <<email>> <- IOException: Connection reset by <email>");
    }

    // Minimal SMTP server that accepts the greeting and rejects the sender, echoing both addresses
    private static async Task RejectSenderAsync(TcpListener listener)
    {
        using var client = await listener.AcceptTcpClientAsync();
        await using var stream = client.GetStream();
        using var reader = new StreamReader(stream);
        await using var writer = new StreamWriter(stream) { NewLine = "\r\n", AutoFlush = true };

        await writer.WriteLineAsync("220 test ESMTP");
        while (await reader.ReadLineAsync() is { } command)
        {
            if (command.StartsWith("QUIT", StringComparison.OrdinalIgnoreCase))
            {
                await writer.WriteLineAsync("221 bye");
                return;
            }

            await writer.WriteLineAsync(command.StartsWith("MAIL", StringComparison.OrdinalIgnoreCase) ? "550 ana@farmacia.pe rejected" : "250 ok");
        }
    }

    private sealed class ListLogger : ILogger<SmtpEmailSender>
    {
        public List<string> Lines { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            // The message id carries the sender domain by design, so it is left out of the checked text
            var text = formatter(state, exception);
            Lines.Add(text[text.IndexOf("Step:", StringComparison.Ordinal)..]);
        }
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
