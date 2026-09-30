using Microsoft.Extensions.Logging;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Infrastructure.Email;

// Development/Testing-only fallback when Email:Provider is None; the message (and its token) is discarded, never logged
public class DisabledEmailSender(ILogger<DisabledEmailSender> logger) : IEmailSender
{
    public Task<Result> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        logger.LogWarning("Email sending is disabled (Email:Provider is None); a message was discarded.");
        return Task.FromResult(Result.Failure(Error.Failure("Email.Disabled", "Email sending is disabled.")));
    }
}
