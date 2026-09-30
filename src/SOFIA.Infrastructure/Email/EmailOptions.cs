namespace SOFIA.Infrastructure.Email;

public class EmailOptions
{
    public const string SectionName = "Email";
    public const string ProviderNone = "None";
    public const string ProviderSmtp = "Smtp";
    public const string ProviderAzureCommunication = "AzureCommunication";

    public string Provider { get; init; } = string.Empty;
    public string FromAddress { get; init; } = string.Empty;
    public string FromDisplayName { get; init; } = "SOFIA";
    public SmtpSettings Smtp { get; init; } = new();
    public AzureCommunicationSettings AzureCommunication { get; init; } = new();

    public bool IsDisabled => string.IsNullOrWhiteSpace(Provider) || Provider.Equals(ProviderNone, StringComparison.OrdinalIgnoreCase);

    public void EnsureValid(bool allowDisabled)
    {
        if (IsDisabled)
        {
            if (!allowDisabled)
            {
                throw new InvalidOperationException("CRITICAL: Email:Provider must be 'Smtp' or 'AzureCommunication' outside Development and Testing; password recovery emails cannot be sent.");
            }

            return;
        }

        if (string.IsNullOrWhiteSpace(FromAddress))
        {
            throw new InvalidOperationException("CRITICAL: Email:FromAddress is not configured.");
        }

        if (Provider.Equals(ProviderSmtp, StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(Smtp.Host) || Smtp.Port is <= 0 or > 65535)
            {
                throw new InvalidOperationException("CRITICAL: Email:Smtp:Host and a valid Email:Smtp:Port are required for the Smtp provider.");
            }

            if (string.IsNullOrEmpty(Smtp.UserName) != string.IsNullOrEmpty(Smtp.Password))
            {
                throw new InvalidOperationException("CRITICAL: Email:Smtp:UserName and Email:Smtp:Password must be set together.");
            }

            return;
        }

        if (Provider.Equals(ProviderAzureCommunication, StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(AzureCommunication.ConnectionString))
            {
                throw new InvalidOperationException("CRITICAL: Email:AzureCommunication:ConnectionString is required for the AzureCommunication provider.");
            }

            return;
        }

        throw new InvalidOperationException($"CRITICAL: Unknown Email:Provider '{Provider}'. Use 'Smtp', 'AzureCommunication' or 'None'.");
    }
}

public class SmtpSettings
{
    public string Host { get; init; } = string.Empty;
    public int Port { get; init; } = 587;
    // StartTLS on 587, implicit TLS on 465; false only for a local catcher such as Mailpit
    public bool UseSsl { get; init; } = true;
    public string? UserName { get; init; }
    public string? Password { get; init; }
}

public class AzureCommunicationSettings
{
    public string ConnectionString { get; init; } = string.Empty;
}
