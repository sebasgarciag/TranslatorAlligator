using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Translator.Core.Interfaces;
using Translator.Core.Models;

namespace Translator.Application.Services
{
    /// <summary>
    /// Implementación principal del servicio de traducción.
    /// Coordina la obtención de traducciones desde la caché o el servicio de IA.
    /// </summary>
    public class TranslationService : ITranslationService
    {
        private readonly IAITranslationClient _aiClient;
        private readonly ICacheProvider _cache;
        private readonly ILogger<TranslationService> _logger;
        private readonly TranslationOptions _options;

        /// <summary>
        /// Constructor para el servicio de traducción.
        /// </summary>
        /// <param name="aiClient">Cliente de traducción de IA</param>
        /// <param name="cache">Proveedor de caché</param>
        /// <param name="options">Opciones de configuración</param>
        /// <param name="logger">Registrador para mensajes de log</param>
        public TranslationService(
            IAITranslationClient aiClient,
            ICacheProvider cache,
            IOptions<TranslationOptions> options,
            ILogger<TranslationService> logger)
        {
            _aiClient = aiClient ?? throw new ArgumentNullException(nameof(aiClient));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Traduce una lista de elementos de texto aplicando lógica de caché.
        /// </summary>
        /// <param name="items">Lista de elementos a traducir</param>
        /// <returns>Lista de resultados de traducción</returns>
        public async Task<List<TranslationResult>> TranslateAsync(List<TranslationItem> items)
        {
            if (items == null || !items.Any())
                return new List<TranslationResult>();

            var results = new List<TranslationResult>();

            foreach (var item in items)
            {
                try
                {
                    // Normaliza el texto y el código de idioma para claves de caché consistentes
                    string normalizedText = item.Text?.Trim() ?? string.Empty;
                    string normalizedLanguage = item.To?.Trim().ToLowerInvariant() ?? string.Empty;
                    
                    // Crea la clave de caché con valores normalizados
                    var cacheKey = $"{normalizedText}|{normalizedLanguage}";
                    
                    // Intenta obtener desde la caché
                    var cachedTranslation = await _cache.GetAsync<string>(cacheKey);
                    
                    string translatedText;
                    
                    if (cachedTranslation != null)
                    {
                        _logger.LogInformation("Acierto en caché para clave: {CacheKey}", cacheKey);
                        translatedText = cachedTranslation;
                    }
                    else
                    {
                        _logger.LogInformation("Fallo en caché para clave: {CacheKey}, llamando al servicio de traducción IA", cacheKey);
                        
                        // Obtiene la traducción del servicio de IA
                        translatedText = await _aiClient.TranslateTextAsync(item.Text, item.To);
                        
                        // Almacena el resultado en caché
                        await _cache.SetAsync(cacheKey, translatedText, _options.CacheTTL);
                    }
                    
                    results.Add(new TranslationResult
                    {
                        Text = item.Text,
                        TranslatedText = translatedText,
                        To = item.To
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al traducir texto: {Text} al idioma: {Language}", item.Text, item.To);
                    
                    // Añade resultado de error
                    results.Add(new TranslationResult
                    {
                        Text = item.Text,
                        TranslatedText = $"ERROR: {ex.Message}",
                        To = item.To
                    });
                }
            }

            return results;
        }
    }

    /// <summary>
    /// Opciones de configuración para el servicio de traducción.
    /// </summary>
    public class TranslationOptions
    {
        /// <summary>
        /// Tiempo de vida (TTL) para entradas en caché. Por defecto: 24 horas.
        /// </summary>
        public TimeSpan CacheTTL { get; set; } = TimeSpan.FromHours(24);
    }
} 