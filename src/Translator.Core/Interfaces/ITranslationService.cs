using System.Collections.Generic;
using System.Threading.Tasks;
using Translator.Core.Models;

namespace Translator.Core.Interfaces
{
    /// <summary>
    /// Interfaz principal para el servicio de traducción.
    /// Define el contrato para los servicios de traducción en la aplicación.
    /// </summary>
    public interface ITranslationService
    {
        /// <summary>
        /// Traduce una lista de elementos de texto al idioma especificado.
        /// </summary>
        /// <param name="items">Lista de elementos a traducir, cada uno con un texto y un idioma de destino</param>
        /// <returns>Lista de resultados de traducción con el texto original, traducido y el idioma de destino</returns>
        Task<List<TranslationResult>> TranslateAsync(List<TranslationItem> items);
    }
} 