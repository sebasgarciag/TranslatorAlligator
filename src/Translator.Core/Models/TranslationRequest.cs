using System.Collections.Generic;
using System.Xml.Serialization;

namespace Translator.Core.Models
{
    /// <summary>
    /// Modelo para la solicitud de traducción.
    /// Utilizado para deserializar las solicitudes JSON y XML.
    /// </summary>
    [XmlRoot("TranslateRequest")]
    public class TranslationRequest
    {
        /// <summary>
        /// Lista de elementos a traducir.
        /// </summary>
        [XmlArray("Items")]
        [XmlArrayItem("Item")]
        public List<TranslationItem> Items { get; set; } = new List<TranslationItem>();
    }

    /// <summary>
    /// Representa un elemento individual a traducir.
    /// </summary>
    public class TranslationItem
    {
        /// <summary>
        /// Texto original a traducir.
        /// </summary>
        [XmlElement("Text")]
        public string Text { get; set; }
        
        /// <summary>
        /// Código del idioma de destino (ej: 'es' para español).
        /// </summary>
        [XmlElement("To")]
        public string To { get; set; }
    }
} 