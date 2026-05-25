using System.Text.Json;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using SOFIA.Application.Common.Interfaces;

namespace SOFIA.Infrastructure.Services;

public class OpenAIRecetaAnalyzer(
    ILogger<OpenAIRecetaAnalyzer> logger,
    IConfiguration configuration,
    IPrivacyService privacyService) : IRecetaAnalyzer
{
    private readonly ILogger<OpenAIRecetaAnalyzer> _logger = logger;
    private readonly IConfiguration _configuration = configuration;
    private readonly IPrivacyService _privacyService = privacyService;

    public async Task<List<MedicamentoInterpretadoDto>> InterpretarRecetaAsync(Stream imagenStream, string? especialidadContexto, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Iniciando extracción de texto mediante GPT-4o Vision...");

            var endpoint = _configuration["AzureOpenAi:Endpoint"] ?? throw new InvalidOperationException("AzureOpenAi:Endpoint is missing.");
            var key = _configuration["AzureOpenAi:Key"] ?? throw new InvalidOperationException("AzureOpenAi:Key is missing.");
            var deploymentName = _configuration["AzureOpenAi:DeploymentName"] ?? "gpt-4o";

            var client = new AzureOpenAIClient(new Uri(endpoint), new System.ClientModel.ApiKeyCredential(key));
            var chatClient = client.GetChatClient(deploymentName);

            using var memoryStream = new MemoryStream();
            await imagenStream.CopyToAsync(memoryStream, cancellationToken);
            var imageBytes = memoryStream.ToArray();

            var visionMessages = new List<ChatMessage>
            {
                new SystemChatMessage("Eres un motor de OCR de alta precisión especializado en recetas médicas. Tu única tarea es extraer TODO el texto visible en la imagen, especialmente el texto manuscrito. No interpretes, solo transcribe exactamente lo que ves."),
                new UserChatMessage(ChatMessageContentPart.CreateImagePart(BinaryData.FromBytes(imageBytes), "image/jpeg"))
            };

            ChatCompletion visionCompletion = await chatClient.CompleteChatAsync(visionMessages, cancellationToken: cancellationToken);
            var textoOcrRaw = visionCompletion.Content[0].Text;

            if (string.IsNullOrWhiteSpace(textoOcrRaw))
            {
                _logger.LogWarning("GPT-4o Vision no pudo extraer texto de la imagen.");
                return [];
            }

            _logger.LogInformation("Texto OCR extraído exitosamente (Longitud: {Len})", textoOcrRaw.Length);

            // Anonimización del texto
            var textoAnomizado = await _privacyService.AnonymizeTextAsync(textoOcrRaw, cancellationToken);

            _logger.LogInformation("Texto OCR anonimizado que se enviará a LLM: {Texto}", textoAnomizado);

            var promptSistema = @"
Eres SOFIA, un asistente farmacéutico experto en el mercado de PERÚ. 
Tu tarea es interpretar el texto extraído (OCR) de una receta médica manuscrita.

IMPORTANTE: El texto OCR es ALTAMENTE RUIDOSO y distorsionado debido a la caligrafía difícil.

Tus objetivos:
1. Identificar medicamentos (Nombre y Concentración) basándote en similitud fonética y contexto de marcas peruanas. 
   Ejemplo: 'DEMADMAN' -> 'EMADRIAN', 'Pro GOLD' -> 'PROVIDE GOLD'.
2. Ignorar etiquetas de anonimización como <PE_DNI>, <PERSON>, <LOCATION> o <DATE_TIME> si aparecen dentro de lo que parece ser el nombre de un producto.
3. Corregir automáticamente errores de lectura (e.g., '10mt' -> '10ml', 'fco' -> 'Frasco').
4. ACTUAR COMO VENDEDOR EXPERTO: Para cada medicamento, sugiere 1 a 3 genéricos/complementarios vendidos en Perú.
5. ASIGNAR CONFIANZA: Decimal 0.0-1.0 según qué tan seguro estés de la reconstrucción del nombre.
6. DIFERENCIAR ENCABEZADOS: Ignora nombres de hospitales, clínicas o médicos que aparezcan al inicio del OCR (ej: 'Monteluz', 'Clinica San Pablo').
   NO confundas nombres de clínicas con medicamentos por similitud fonética (ej: 'Monteluz' NO es 'Montelukast').
   Los medicamentos genuinos suelen tener dosis asociadas (ej: 500mg, 1 tab, 1 c, 1 sol) o instrucciones de frecuencia (ej: cada 8h, c/die).

Responde ESTRICTAMENTE en formato JSON:
{
  ""medicamentos"": [
      { 
        ""NombreDetectado"": ""Nombre Corregido"",
        ""ConcentracionDetectada"": ""Dosis/Presentación"",
        ""Sugerencias"": [""Sugerencia 1""],
        ""NivelConfianza"": 0.8
      }
  ]
}

Si no hay NINGUNA palabra que parezca un medicamento, responde: { ""medicamentos"": [] }";

            var promptUsuario = $@"
Contexto adicional: {(string.IsNullOrEmpty(especialidadContexto) ? "Medicina General" : especialidadContexto)}.
Texto OCR Sucio: {textoAnomizado}";

            var completionOptions = new ChatCompletionOptions
            {
                Temperature = 0.2f,
                ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
            };

            ChatCompletion completion = await chatClient.CompleteChatAsync(
                [
                    new SystemChatMessage(promptSistema),
                    new UserChatMessage(promptUsuario)
                ],
                completionOptions,
                cancellationToken: cancellationToken
            );

            var respuestaJson = completion.Content[0].Text;
            _logger.LogInformation("Respuesta Raw OpenAI: {Json}", respuestaJson);

            respuestaJson = respuestaJson.Replace("```json", "").Replace("```", "").Trim();

            using var doc = JsonDocument.Parse(respuestaJson);

            if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                doc.RootElement.TryGetProperty("medicamentos", out var propiedadMedicamentos))
            {
                var opciones = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var lista = JsonSerializer.Deserialize<List<MedicamentoInterpretadoDto>>(propiedadMedicamentos.GetRawText(), opciones);

                return lista ?? [];
            }

            _logger.LogWarning("La IA devolvió un JSON válido pero sin la propiedad 'medicamentos'. Retornando lista vacía.");
            return [];
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error al deserializar la respuesta de OpenAI.");
            return [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error general en InterpretarRecetaAsync.");
            throw;
        }
    }
}
