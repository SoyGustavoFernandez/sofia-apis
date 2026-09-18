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

            if (string.IsNullOrEmpty(result?.TextoLimpio))
            {
                throw new InvalidOperationException("Presidio returned an empty anonymization result.");
            }

            return result.TextoLimpio;
        }
        catch (Exception ex)
        {
            // Fail-closed: patient text must never reach an external LLM unanonymized.
            _logger.LogError(ex, "Critical error connecting to Microsoft Presidio; refusing to process unanonymized patient data.");
            throw new InvalidOperationException("Failed to anonymize patient text via Presidio.", ex);
        }
    }

    private sealed class PresidioAnonymizerResponse
    {
        [JsonPropertyName("TextoLimpio")]
        public string? TextoLimpio { get; set; }
    }
}
