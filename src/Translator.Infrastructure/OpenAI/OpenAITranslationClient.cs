using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Translator.Core.Interfaces;

namespace Translator.Infrastructure.OpenAI
{
    /// <summary>
    /// Cliente para la API de OpenAI que se utiliza para traducciones.
    /// Implementa la interfaz IAITranslationClient y maneja la comunicación con la API de OpenAI.
    /// </summary>
    public class OpenAITranslationClient : IAITranslationClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OpenAITranslationClient> _logger;
        private readonly OpenAIOptions _options;

        /// <summary>
        /// Constructor para el cliente de OpenAI.
        /// </summary>
        /// <param name="httpClient">Cliente HTTP para realizar solicitudes a la API</param>
        /// <param name="options">Opciones de configuración para OpenAI</param>
        /// <param name="logger">Registrador para mensajes de log</param>
        public OpenAITranslationClient(
            HttpClient httpClient,
            IOptions<OpenAIOptions> options,
            ILogger<OpenAITranslationClient> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Configura el cliente HTTP
            _httpClient.BaseAddress = new Uri("https://api.openai.com/");
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_options.ApiKey}");
        }

        /// <summary>
        /// Traduce un texto al idioma especificado utilizando la API de OpenAI.
        /// </summary>
        /// <param name="text">Texto a traducir</param>
        /// <param name="targetLanguage">Código del idioma de destino</param>
        /// <returns>Texto traducido</returns>
        public async Task<string> TranslateTextAsync(string text, string targetLanguage)
        {
            try
            {
                _logger.LogInformation("Traduciendo texto a {TargetLanguage}: \"{Text}\"", targetLanguage, text);

                var prompt = $"Translate the following sentence into {targetLanguage}: \"{text}\"";
                
                var requestBody = new
                {
                    model = _options.ModelName,
                    messages = new[]
                    {
                        new { role = "system", content = "You are a professional translator. Respond with only the translation, no explanations or additional text." },
                        new { role = "user", content = prompt }
                    },
                    temperature = 0.3
                };

                // Serializa y envía la solicitud
                var requestJson = JsonSerializer.Serialize(requestBody);
                _logger.LogDebug("Solicitud a OpenAI: {Request}", requestJson);
                
                var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

                // Aplica lógica de reintento con retroceso exponencial
                int maxRetries = 3;
                int retryCount = 0;
                int retryDelay = 500; // ms

                while (true)
                {
                    try
                    {
                        var response = await _httpClient.PostAsync("v1/chat/completions", content);
                        
                        var responseJson = await response.Content.ReadAsStringAsync();
                        _logger.LogInformation("Respuesta de OpenAI: {Response}", responseJson);
                        
                        if (!response.IsSuccessStatusCode)
                        {
                            _logger.LogError("Error en API de OpenAI: {StatusCode} - {Response}", 
                                response.StatusCode, responseJson);
                            return $"ERROR: Error en API de OpenAI {response.StatusCode}";
                        }

                        // Extracción manual del texto traducido del JSON
                        string translatedText = null;
                        
                        try
                        {
                            using (JsonDocument doc = JsonDocument.Parse(responseJson))
                            {
                                var root = doc.RootElement;
                                if (root.TryGetProperty("choices", out var choices) && 
                                    choices.GetArrayLength() > 0)
                                {
                                    var firstChoice = choices[0];
                                    if (firstChoice.TryGetProperty("message", out var message) &&
                                        message.TryGetProperty("content", out var content_value))
                                    {
                                        translatedText = content_value.GetString();
                                        _logger.LogInformation("Contenido extraído: '{Content}'", translatedText);
                                        
                                        // Limpiar comillas adicionales de la traducción
                                        translatedText = translatedText?.Trim();
                                        if (translatedText?.StartsWith("\"") == true && translatedText.EndsWith("\""))
                                        {
                                            translatedText = translatedText.Substring(1, translatedText.Length - 2);
                                            _logger.LogInformation("Comillas eliminadas. Texto limpio: '{Content}'", translatedText);
                                        }
                                    }
                                    else
                                    {
                                        _logger.LogWarning("No se pudo encontrar la propiedad message.content en la respuesta");
                                    }
                                }
                                else
                                {
                                    _logger.LogWarning("No se pudo encontrar el array choices en la respuesta");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error durante la extracción manual de JSON");
                        }
                        
                        if (string.IsNullOrEmpty(translatedText))
                        {
                            _logger.LogWarning("Se recibió una traducción vacía de OpenAI");
                            return "ERROR: Se recibió una traducción vacía";
                        }

                        _logger.LogInformation("Traducción exitosa: {OriginalText} → {TranslatedText}", 
                            text, translatedText);
                        return translatedText;
                    }
                    catch (Exception ex) when (retryCount < maxRetries && 
                                               (ex is HttpRequestException || 
                                                ex is TaskCanceledException || 
                                                ex is TimeoutException))
                    {
                        retryCount++;
                        _logger.LogWarning(ex, "Error en llamada a API de OpenAI (intento {RetryCount}/{MaxRetries}), reintentando en {RetryDelay}ms", 
                            retryCount, maxRetries, retryDelay);
                        
                        await Task.Delay(retryDelay);
                        retryDelay *= 2; // Retroceso exponencial
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error en llamada a API de OpenAI: {ErrorMessage}", ex.Message);
                        if (ex is JsonException)
                        {
                            _logger.LogError("Error de deserialización JSON. El formato de respuesta puede haber cambiado");
                        }
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al traducir texto con OpenAI");
                throw;
            }
        }
    }

    /// <summary>
    /// Opciones de configuración para la integración con OpenAI.
    /// </summary>
    public class OpenAIOptions
    {
        /// <summary>
        /// Clave de API para OpenAI.
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Nombre del modelo a utilizar. Por defecto: "gpt-3.5-turbo".
        /// </summary>
        public string ModelName { get; set; } = "gpt-3.5-turbo";
    }

    public class OpenAIResponse
    {
        public OpenAIChoice[]? Choices { get; set; }
    }

    public class OpenAIChoice
    {
        public OpenAIMessage? Message { get; set; }
    }

    public class OpenAIMessage
    {
        public string? Content { get; set; }
    }
} 