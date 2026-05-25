namespace SOFIA.Application.Common.Interfaces;

public interface IPrivacyService
{
    Task<string> AnonymizeTextAsync(string rawText, CancellationToken cancellationToken = default);
}
