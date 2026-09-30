using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Infrastructure.Email;

public class SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public static readonly Error SendFailedError = Error.Failure("Email.SendFailed", "The email could not be sent.");

    public async Task<Result> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        var mime = CreateMimeMessage(settings, message);

        string? failure = null;
        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(settings.Smtp.Host, settings.Smtp.Port, SocketOptions(settings.Smtp), cancellationToken);
            if (!string.IsNullOrEmpty(settings.Smtp.UserName))
            {
                await client.AuthenticateAsync(settings.Smtp.UserName, settings.Smtp.Password ?? string.Empty, cancellationToken);
            }

            _ = await client.SendAsync(mime, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Only the exception type is kept: SMTP error texts can echo the recipient address
            failure = ex.GetType().Name;
        }

        if (failure is not null)
        {
            logger.LogError("Email could not be sent via SMTP. MessageId: {MessageId}. Error: {ErrorType}", mime.MessageId, failure);
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
}
