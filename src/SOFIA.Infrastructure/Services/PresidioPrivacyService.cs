using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using SOFIA.Application.Common.Interfaces;

namespace SOFIA.Infrastructure.Services;

public class PresidioPrivacyService(
    HttpClient httpClient,
    ILogger<PresidioPrivacyService> logger,
    TimeSpan? retryDelay = null,
    TimeSpan? retryBudget = null) : IPrivacyService
{
    // Presidio scales to zero when idle, so the first call waits for it to start instead of failing
    private static readonly TimeSpan DefaultRetryDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan DefaultRetryBudget = TimeSpan.FromSeconds(60);

    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<PresidioPrivacyService> _logger = logger;
    private readonly TimeSpan _retryDelay = retryDelay ?? DefaultRetryDelay;
    private readonly TimeSpan _retryBudget = retryBudget ?? DefaultRetryBudget;

    public async Task<string> AnonymizeTextAsync(string rawText, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return rawText;
        }

        try
        {
            using var response = await PostUntilAvailableAsync(rawText, cancellationToken);
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

    // Only "not available yet" outcomes are retried; any other answer is returned to the caller at once
    private async Task<HttpResponseMessage> PostUntilAvailableAsync(string rawText, CancellationToken cancellationToken)
    {
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        budget.CancelAfter(_retryBudget);
        var request = new { texto = rawText };

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/api/anonimizar", request, budget.Token);
                if (!IsStarting(response.StatusCode))
                {
                    return response;
                }

                _logger.LogWarning("Presidio is not available yet (attempt {Attempt}, status {Status}).", attempt, (int)response.StatusCode);
                response.Dispose();
            }
            catch (HttpRequestException ex) when (ex.StatusCode is null)
            {
                _logger.LogWarning("Presidio could not be reached yet (attempt {Attempt}, error {Error}).", attempt, ex.HttpRequestError);
            }

            await Task.Delay(_retryDelay, budget.Token);
        }
    }

    private static bool IsStarting(HttpStatusCode status) =>
        status is HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout;

    private sealed class PresidioAnonymizerResponse
    {
        [JsonPropertyName("TextoLimpio")]
        public string? TextoLimpio { get; set; }
    }
}
