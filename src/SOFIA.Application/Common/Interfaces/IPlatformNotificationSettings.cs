namespace SOFIA.Application.Common.Interfaces;

public interface IPlatformNotificationSettings
{
    // Empty when nobody is to be told
    string OwnerRecipient { get; }
}
