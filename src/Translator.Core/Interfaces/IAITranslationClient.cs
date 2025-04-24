using System.Threading.Tasks;

namespace Translator.Core.Interfaces
{
    /// <summary>
    /// Interfaz para el cliente de traducción basado en IA.
    /// Define el contrato para servicios de traducción basados en inteligencia artificial.
    /// </summary>
    public interface IAITranslationClient
    {
        /// <summary>
        /// Traduce un texto al idioma especificado utilizando servicios de IA.
        /// </summary>
        /// <param name="text">Texto a traducir</param>
        /// <param name="targetLanguage">Código del idioma de destino (ej: 'es', 'fr', 'de')</param>
        /// <returns>Texto traducido</returns>
        Task<string> TranslateTextAsync(string text, string targetLanguage);
    }
} 