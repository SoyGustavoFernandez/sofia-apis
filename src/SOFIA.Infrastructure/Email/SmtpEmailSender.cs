using System.Text.RegularExpressions;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Infrastructure.Email;

public partial class SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private const int MaxDetailLength = 500;

    public static readonly Error SendFailedError = Error.Failure("Email.SendFailed", "The email could not be sent.");

    public async Task<Result> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        var mime = CreateMimeMessage(settings, message);

        (string Type, string Detail)? failure = null;
        var step = "Connect";
        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(settings.Smtp.Host, settings.Smtp.Port, SocketOptions(settings.Smtp), cancellationToken);
            if (!string.IsNullOrEmpty(settings.Smtp.UserName))
            {
                step = "Authenticate";
                await client.AuthenticateAsync(settings.Smtp.UserName, settings.Smtp.Password ?? string.Empty, cancellationToken);
            }

            // Closing the session counts as part of sending
            step = "Send";
            _ = await client.SendAsync(mime, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            failure = (ex.GetType().Name, Describe(ex));
        }

        if (failure is { } f)
        {
            logger.LogError("Email could not be sent via SMTP. MessageId: {MessageId}. Step: {Step}. Error: {ErrorType}. Detail: {Detail}", mime.MessageId, step, f.Type, f.Detail);
            return Result.Failure(SendFailedError);
        }

        logger.LogInformation("Email sent via SMTP. MessageId: {MessageId}", mime.MessageId);
        return Result.Success();
    }

    public static MimeMessage CreateMimeMessage(EmailOptions settings, EmailMessage message)
    {
        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(settings.FromDisplayName, settings.FromAddress));
        mime.To.Add(MailboxAddress.Parse(message.To));
        mime.Subject = message.Subject;
        mime.MessageId = MimeKit.Utils.MimeUtils.GenerateMessageId(MailboxAddress.Parse(settings.FromAddress).Domain);
        mime.Body = new BodyBuilder { HtmlBody = message.HtmlBody, TextBody = message.TextBody }.ToMessageBody();
        return mime;
    }

    public static SecureSocketOptions SocketOptions(SmtpSettings smtp) => smtp switch
    {
        { UseSsl: false } => SecureSocketOptions.None,
        { Port: 465 } => SecureSocketOptions.SslOnConnect,
        _ => SecureSocketOptions.StartTls,
    };

    // Server texts can echo mail addresses, so every one of them is masked before the failure reaches the log
    public static string Describe(Exception exception)
    {
        var parts = new List<string>();
        for (var current = exception; current is not null; current = current.InnerException)
        {
            var status = current is SmtpCommandException command ? $" [{(int)command.StatusCode}]" : string.Empty;
            parts.Add($"{current.GetType().Name}{status}: {current.Message}");
        }

        var detail = AddressRegex().Replace(string.Join(" <- ", parts), "<email>");
        return detail.Length <= MaxDetailLength ? detail : detail[..MaxDetailLength];
    }

    [GeneratedRegex(@"[^\s<>""'(),;:\[\]]+@[^\s<>""'(),;:\[\]]+")]
    private static partial Regex AddressRegex();
}
