namespace SOFIA.Infrastructure.Email;

public class NotificationOptions
{
    public const string SectionName = "Notifications";

    // The platform owner's address for new companies and expiry summaries; empty turns those off
    public string OwnerRecipient { get; init; } = string.Empty;

    // Shared secret the daily scheduled task sends; empty keeps the task endpoint closed
    public string TasksKey { get; init; } = string.Empty;
}
