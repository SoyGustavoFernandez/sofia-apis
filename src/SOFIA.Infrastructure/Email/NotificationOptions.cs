namespace SOFIA.Infrastructure.Email;

public class NotificationOptions
{
    public const string SectionName = "Notifications";

    // Who is told about every new company; empty turns the notice off
    public string NewCompanyRecipient { get; init; } = string.Empty;
}
