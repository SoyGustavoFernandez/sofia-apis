using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;
using SOFIA.Infrastructure.Email;
using SOFIA.IntegrationTests.Infrastructure;

namespace SOFIA.IntegrationTests.Email;

public class NuevaEmpresaNotifierTests(SofiaWebAppFactory factory) : IClassFixture<SofiaWebAppFactory>
{
    private static readonly NuevaEmpresaAviso Aviso = new(
        "Botica <b>Sol</b>\r\nBcc: x@y.pe", "20123456789", "Ana Quispe", "ana@botica.test", new DateTimeOffset(2026, 11, 26, 18, 30, 0, TimeSpan.Zero));

    private sealed class HangingSender : IEmailSender
    {
        public async Task<Result> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return Result.Success();
        }
    }

    private static NuevaEmpresaNotifier Notifier(IEmailSender sender, string destinatario, TimeSpan? timeout = null) => new(
        sender, Options.Create(new NotificationOptions { NewCompanyRecipient = destinatario }), NullLogger<NuevaEmpresaNotifier>.Instance, timeout);

    [Fact]
    public async Task Notificar_ShouldMailTheOwnerWithTheDataEncodedAndTheSubjectOnOneLine()
    {
        var sender = new CapturingEmailSender();

        await Notifier(sender, "duenio@sofia.test").NotificarAsync(Aviso);

        var mensaje = sender.Messages.Should().ContainSingle().Subject;
        _ = mensaje.To.Should().Be("duenio@sofia.test");
        _ = mensaje.Subject.Should().NotContainAny("\r", "\n");
        _ = mensaje.HtmlBody.Should().Contain("Botica &lt;b&gt;Sol&lt;/b&gt;").And.Contain("20123456789").And.Contain("26/11/2026 13:30");
        _ = mensaje.TextBody.Should().Contain("ana@botica.test");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Notificar_ShouldSendNothing_WhenNoRecipientIsConfigured(string destinatario)
    {
        var sender = new CapturingEmailSender();

        await Notifier(sender, destinatario).NotificarAsync(Aviso);

        _ = sender.Messages.Should().BeEmpty();
    }

    [Fact]
    public async Task Notificar_ShouldReturnWithoutThrowing_WhenTheMailServerHangs()
    {
        var act = () => Notifier(new HangingSender(), "duenio@sofia.test", TimeSpan.FromMilliseconds(50)).NotificarAsync(Aviso);

        _ = await act.Should().NotThrowAsync();
    }

    [Fact]
    public void Notifier_ShouldBeBuiltByTheApplicationContainer() =>
        _ = factory.Services.GetRequiredService<INuevaEmpresaNotifier>().Should().BeOfType<NuevaEmpresaNotifier>();
}
