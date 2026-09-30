using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using AcsEmailMessage = Azure.Communication.Email.EmailMessage;
using EmailMessage = SOFIA.Application.Common.Models.EmailMessage;

namespace SOFIA.Infrastructure.Email;

public class AzureCommunicationEmailSender(EmailClient client, IOptions<EmailOptions> options, ILogger<AzureCommunicationEmailSender> logger) : IEmailSender
{
    public async Task<Result> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        EmailSendOperation? operation = null;
        (int Status, string? ErrorCode)? failure = null;
        try
        {
            // Started, not Completed: ACS accepts the message and delivers it asynchronously, so the request is not held open
            operation = await client.SendAsync(WaitUntil.Started, CreateMessage(options.Value, message), cancellationToken);
        }
        catch (RequestFailedException ex)
        {
            // Only status and code are kept: the service's error text can echo the recipient address
            failure = (ex.Status, ex.ErrorCode);
        }

        if (failure is { } f)
        {
            logger.LogError("Azure Communication Services rejected the email. Status: {Status}. ErrorCode: {ErrorCode}", f.Status, f.ErrorCode);
            return Result.Failure(SmtpEmailSender.SendFailedError);
        }

        logger.LogInformation("Email accepted by Azure Communication Services. OperationId: {OperationId}", operation!.Id);
        return Result.Success();
    }

    // The display name comes from the sender configured on the ACS email domain, not from the message
    public static AcsEmailMessage CreateMessage(EmailOptions settings, EmailMessage message) =>
        new(
            settings.FromAddress,
            message.To,
            new EmailContent(message.Subject) { Html = message.HtmlBody, PlainText = message.TextBody });
}
