using Microsoft.Extensions.Options;
using SOFIA.Application.Common.Interfaces;

namespace SOFIA.Infrastructure.Email;

public class PlatformNotificationSettings(IOptions<NotificationOptions> options) : IPlatformNotificationSettings
{
    public string OwnerRecipient => options.Value.OwnerRecipient;
}
