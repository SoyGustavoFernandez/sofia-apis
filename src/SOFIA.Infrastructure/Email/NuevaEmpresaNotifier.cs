using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Application.Empresas.Notifications;

namespace SOFIA.Infrastructure.Email;

public class NuevaEmpresaNotifier(
    IEmailSender emailSender,
    IOptions<NotificationOptions> options,
    ILogger<NuevaEmpresaNotifier> logger,
    TimeSpan? timeout = null) : INuevaEmpresaNotifier
{
    // The person who just signed up is waiting for this call, so a slow mail server is cut short
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

    public async Task NotificarAsync(NuevaEmpresaAviso aviso)
    {
        var destinatario = options.Value.NewCompanyRecipient;
        if (string.IsNullOrWhiteSpace(destinatario))
        {
            return;
        }

        var fallo = await EnviarAsync(destinatario, aviso);
        if (fallo is not null)
        {
            logger.LogWarning("The new company notice could not be sent: {Reason}", fallo);
        }
    }

    // Only the failure code or type is kept: mail error texts can echo addresses
    private async Task<string?> EnviarAsync(string destinatario, NuevaEmpresaAviso aviso)
    {
        try
        {
            using var limite = new CancellationTokenSource(timeout ?? DefaultTimeout);
            var enviado = await emailSender.SendAsync(NuevaEmpresaEmail.Build(destinatario, aviso), limite.Token);
            return enviado.IsFailure ? enviado.Error.Code : null;
        }
        catch (Exception ex)
        {
            return ex.GetType().Name;
        }
    }
}
