using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SOFIA.Application.Common.Interfaces;

namespace SOFIA.Infrastructure.Services;

public class GeminiRecetaAnalyzer(
    ILogger<GeminiRecetaAnalyzer> logger,
    IConfiguration configuration,
    IPrivacyService privacyService,
    HttpClient httpClient) : IRecetaAnalyzer
{

    private readonly ILogger<GeminiRecetaAnalyzer> _logger = logger;
    private readonly IConfiguration _configuration = configuration;
    private readonly IPrivacyService _privacyService = privacyService;
    private readonly HttpClient _httpClient = httpClient;

    public async Task<List<MedicamentoInterpretadoDto>> InterpretarRecetaAsync(Stream imagenStream, string? especialidadContexto, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting text extraction via Gemini 1.5 API...");

            var apiKey = _configuration["GeminiApi:ApiKey"] ?? throw new InvalidOperationException("GeminiApi:ApiKey is missing.");

            // Convert image to base64
            using var memoryStream = new MemoryStream();
            await imagenStream.CopyToAsync(memoryStream, cancellationToken);
            var imageBytes = memoryStream.ToArray();
            var base64Image = Convert.ToBase64String(imageBytes);

            // PASS 1: OCR with Gemini Flash
            var ocrText = await ExtractTextFromImageAsync(base64Image, apiKey, cancellationToken);

            if (string.IsNullOrWhiteSpace(ocrText))
            {
                _logger.LogWarning("Gemini could not extract text from the image.");
                return [];
            }

            _logger.LogInformation("OCR text extracted successfully (Length: {Len})", ocrText.Length);

            // Anonymize the text
            var textoAnomizado = await _privacyService.AnonymizeTextAsync(ocrText, cancellationToken);

            _logger.LogInformation("Anonymized OCR text to be sent to the LLM: {Texto}", textoAnomizado);

            // PASS 2: Reasoning with Gemini Pro/Flash
            var listaMedicamentos = await AnalyzeTextWithGeminiAsync(textoAnomizado, especialidadContexto, apiKey, cancellationToken);

            return listaMedicamentos;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to analyze prescription image.", ex);
        }
    }

    private async Task<string> ExtractTextFromImageAsync(string base64Image, string apiKey, CancellationToken cancellationToken)
    {
        var model = _configuration["GeminiApi:OcrModel"] ?? "gemini-3.6-flash";
        var baseUrl = _configuration["GeminiApi:BaseUrl"] ?? throw new InvalidOperationException("GeminiApi:BaseUrl is missing.");
        var url = $"{baseUrl}{model}:generateContent";

        var payload = new
        {
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new { text = "Eres un motor de OCR de alta precisión especializado en recetas médicas. Tu única tarea es extraer TODO el texto visible en la imagen, especialmente el texto manuscrito. No interpretes, solo transcribe exactamente lo que ves." },
                        new { inline_data = new { mime_type = "image/jpeg", data = base64Image } }
                    }
                }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Add("x-goog-api-key", apiKey);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessOrThrowAsync(response, "OCR", cancellationToken);

        var jsonResponse = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: cancellationToken);
        var extractedText = jsonResponse?.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text").GetString();

        return extractedText ?? string.Empty;
    }

    private async Task<List<MedicamentoInterpretadoDto>> AnalyzeTextWithGeminiAsync(string textoAnomizado, string? especialidadContexto, string apiKey, CancellationToken cancellationToken)
    {
        var model = _configuration["GeminiApi:OcrModel"] ?? "gemini-3.6-flash";
        var baseUrl = _configuration["GeminiApi:BaseUrl"] ?? throw new InvalidOperationException("GeminiApi:BaseUrl is missing.");
        var url = $"{baseUrl}{model}:generateContent";

        var promptSistema = @"
Eres SOFIA, un asistente farmacéutico experto en el mercado de PERÚ. 
Tu tarea es interpretar el texto extraído (OCR) de una receta médica manuscrita.

IMPORTANTE: El texto OCR es ALTAMENTE RUIDOSO y distorsionado debido a la caligrafía difícil.

Tus objetivos:
1. Identificar medicamentos (Nombre y Concentración) basándote en similitud fonética y contexto de marcas peruanas.
2. Ignorar etiquetas de anonimización como <PE_DNI>, <PERSON>, <LOCATION> o <DATE_TIME> si aparecen dentro de lo que parece ser el nombre de un producto.
3. Corregir automáticamente errores de lectura (e.g., '10mt' -> '10ml', 'fco' -> 'Frasco').
4. ACTUAR COMO VENDEDOR EXPERTO: Para cada medicamento, sugiere 1 a 3 genéricos/complementarios vendidos en Perú.
5. ASIGNAR CONFIANZA: Decimal 0.0-1.0 según qué tan seguro estés de la reconstrucción del nombre.
6. DIFERENCIAR ENCABEZADOS: Ignora nombres de hospitales o clínicas.
7. EXTRAER CANTIDAD: Si el texto indica una cantidad numérica de unidades a dispensar (e.g., '30 tabletas' -> 30, 'x 1 frasco' -> 1), inclúyela como número entero. Si no hay cantidad explícita, usa null.

Responde ESTRICTAMENTE en formato JSON:
{
  ""medicamentos"": [
      {
        ""NombreDetectado"": ""Nombre Corregido"",
        ""ConcentracionDetectada"": ""Dosis/Presentación"",
        ""CantidadSugerida"": 30,
        ""Sugerencias"": [""Sugerencia 1""],
        ""NivelConfianza"": 0.8
      }
  ]
}

Si no hay NINGUNA palabra que parezca un medicamento, responde: { ""medicamentos"": [] }";

        var promptUsuario = $@"
Contexto adicional: {(string.IsNullOrEmpty(especialidadContexto) ? "Medicina General" : especialidadContexto)}.
Texto OCR Sucio: {textoAnomizado}";

        var payload = new
        {
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[] { new { text = promptSistema + "\n\n" + promptUsuario } }
                }
            },
            generationConfig = new
            {
                temperature = 0.2,
                response_mime_type = "application/json"
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Add("x-goog-api-key", apiKey);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessOrThrowAsync(response, "Reasoning", cancellationToken);

        var jsonResponse = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: cancellationToken);
        var responseText = jsonResponse?.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text").GetString();

        if (string.IsNullOrWhiteSpace(responseText))
        {
            return [];
        }

        try
        {
            using var doc = JsonDocument.Parse(responseText);
            if (doc.RootElement.TryGetProperty("medicamentos", out var propiedadMedicamentos))
            {
                var opciones = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var lista = JsonSerializer.Deserialize<List<MedicamentoInterpretadoDto>>(propiedadMedicamentos.GetRawText(), opciones);
                return lista ?? [];
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize Gemini JSON response.");
        }

        return [];
    }

    private async Task EnsureSuccessOrThrowAsync(HttpResponseMessage response, string context, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogError(
            "Gemini API call failed ({Context}). Status: {StatusCode}. Body: {Body}",
            context,
            (int)response.StatusCode,
            errorBody);

        throw new HttpRequestException(
            $"Gemini API request failed ({context}) with status {(int)response.StatusCode} ({response.StatusCode}): {errorBody}",
            null,
            response.StatusCode);
    }
}
