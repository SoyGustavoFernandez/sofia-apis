using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using SOFIA.Application.Common.Interfaces;

namespace SOFIA.Infrastructure.Services;

public class PresidioPrivacyService(HttpClient httpClient, ILogger<PresidioPrivacyService> logger) : IPrivacyService
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<PresidioPrivacyService> _logger = logger;

    public async Task<string> AnonymizeTextAsync(string rawText, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return rawText;
        }

        try
        {
            var request = new { texto = rawText };

            var response = await _httpClient.PostAsJsonAsync("/api/anonimizar", request, cancellationToken);
            _ = response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<PresidioAnonymizerResponse>(cancellationToken: cancellationToken);

            return result?.TextoLimpio ?? rawText;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error connecting to Microsoft Presidio. Returning original text.");
            // In production this could throw (Fail-Closed). For dev safety we return the original.
            return rawText;
        }
    }

    private sealed class PresidioAnonymizerResponse
    {
        [JsonPropertyName("TextoLimpio")]
        public string? TextoLimpio { get; set; }
    }
}
