using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging.Abstractions;
using SOFIA.Infrastructure.Services;

namespace SOFIA.IntegrationTests.Recetas;

public class PresidioPrivacyServiceTests
{
    // Answers with the queued outcomes in order and repeats the last one; null means the connection fails
    private sealed class ScriptedHandler(params HttpStatusCode?[] outcomes) : HttpMessageHandler
    {
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var outcome = outcomes[Math.Min(Calls, outcomes.Length - 1)];
            Calls++;
            return outcome is { } status
                ? Task.FromResult(new HttpResponseMessage(status) { Content = JsonContent.Create(new { TextoLimpio = "texto limpio" }) })
                : Task.FromException<HttpResponseMessage>(new HttpRequestException("Connection refused"));
        }
    }

    private static PresidioPrivacyService Service(ScriptedHandler handler, int delayMs = 1, int budgetMs = 30_000) => new(
        new HttpClient(handler) { BaseAddress = new Uri("http://presidio.test") },
        NullLogger<PresidioPrivacyService>.Instance,
        TimeSpan.FromMilliseconds(delayMs),
        TimeSpan.FromMilliseconds(budgetMs));

    [Fact]
    public async Task Anonymize_ShouldWaitForTheService_WhenItIsStillStarting()
    {
        var handler = new ScriptedHandler(HttpStatusCode.ServiceUnavailable, null, HttpStatusCode.BadGateway, HttpStatusCode.OK);

        var result = await Service(handler).AnonymizeTextAsync("Juan Perez");

        _ = result.Should().Be("texto limpio");
        _ = handler.Calls.Should().Be(4);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task Anonymize_ShouldFailAtOnce_WhenTheServiceAnswersWithAnError(HttpStatusCode status)
    {
        var handler = new ScriptedHandler(status);

        var act = () => Service(handler).AnonymizeTextAsync("Juan Perez");

        _ = await act.Should().ThrowAsync<InvalidOperationException>();
        _ = handler.Calls.Should().Be(1);
    }

    [Fact]
    public async Task Anonymize_ShouldGiveUpWithoutReturningTheRawText_WhenTheWaitIsExhausted()
    {
        var handler = new ScriptedHandler(HttpStatusCode.ServiceUnavailable);

        var act = () => Service(handler, delayMs: 20, budgetMs: 200).AnonymizeTextAsync("Juan Perez");

        _ = await act.Should().ThrowAsync<InvalidOperationException>();
        _ = handler.Calls.Should().BeGreaterThan(1);
    }
}
