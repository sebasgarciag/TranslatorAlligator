using System.Collections.Generic;
using System.Xml.Serialization;

namespace Translator.Core.Models
{
    /// <summary>
    /// Modelo para la respuesta de traducción.
    /// Utilizado para serializar las respuestas en formatos JSON y XML.
    /// </summary>
    [XmlRoot("TranslationResponse")]
    public class TranslationResponse
    {
        /// <summary>
        /// Lista de resultados de traducción.
        /// </summary>
        [XmlArray("Results")]
        [XmlArrayItem("Result")]
        public List<TranslationResult> Results { get; set; } = new List<TranslationResult>();
    }

    /// <summary>
    /// Representa un resultado individual de traducción.
    /// </summary>
    public class TranslationResult
    {
        /// <summary>
        /// Texto original que se tradujo.
        /// </summary>
        [XmlElement("Text")]
        public string Text { get; set; }
        
        /// <summary>
        /// Texto traducido al idioma de destino.
        /// </summary>
        [XmlElement("TranslatedText")]
        public string TranslatedText { get; set; }
        
        /// <summary>
        /// Código del idioma al que se tradujo el texto.
        /// </summary>
        [XmlElement("To")]
        public string To { get; set; }
    }
} 