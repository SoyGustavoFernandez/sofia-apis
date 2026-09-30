using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Infrastructure.Services;

namespace SOFIA.IntegrationTests.Recetas;

public class GeminiRecetaAnalyzerTests
{
    private const string BaseUrl = "https://gemini.test/v1beta/models/";

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public List<Uri> RequestedUris { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestedUris.Add(request.RequestUri!);
            // First call is OCR (plain text), second is the analysis (JSON payload inside the text part)
            var text = RequestedUris.Count == 1 ? "Amoxicilina 500mg" : /*lang=json,strict*/ """{"medicamentos":[]}""";
            var body = JsonSerializer.Serialize(new { candidates = new[] { new { content = new { parts = new[] { new { text } } } } } });
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") });
        }
    }

    private sealed class PassThroughPrivacyService : IPrivacyService
    {
        public Task<string> AnonymizeTextAsync(string rawText, CancellationToken cancellationToken = default) => Task.FromResult(rawText);
    }

    private static async Task<List<Uri>> AnalyzeAsync(string? ocrModel, string? reasoningModel)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["GeminiApi:ApiKey"] = "test-key",
            ["GeminiApi:BaseUrl"] = BaseUrl,
            ["GeminiApi:OcrModel"] = ocrModel,
            ["GeminiApi:ReasoningModel"] = reasoningModel
        }).Build();
        var handler = new RecordingHandler();
        using var httpClient = new HttpClient(handler);
        var analyzer = new GeminiRecetaAnalyzer(NullLogger<GeminiRecetaAnalyzer>.Instance, configuration, new PassThroughPrivacyService(), httpClient);

        using var image = new MemoryStream([1, 2, 3]);
        _ = await analyzer.InterpretarRecetaAsync(image, null);

        return handler.RequestedUris;
    }

    [Fact]
    public async Task InterpretarRecetaAsync_ShouldUseOcrModelForOcrAndReasoningModelForAnalysis()
    {
        var uris = await AnalyzeAsync("ocr-model", "reasoning-model");

        _ = uris.Select(u => u.ToString()).Should().Equal(
            $"{BaseUrl}ocr-model:generateContent",
            $"{BaseUrl}reasoning-model:generateContent");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task InterpretarRecetaAsync_ShouldFallBackToOcrModel_WhenReasoningModelIsNotSet(string? reasoningModel)
    {
        var uris = await AnalyzeAsync("ocr-model", reasoningModel);

        _ = uris[1].ToString().Should().Be($"{BaseUrl}ocr-model:generateContent");
    }
}
