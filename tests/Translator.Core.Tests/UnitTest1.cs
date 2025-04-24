using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using Translator.Core.Models;
using Xunit;

namespace Translator.Core.Tests
{
    public class SerializationTests
    {
        [Fact]
        public void TranslationRequest_XmlSerialization_DeserializesCorrectly()
        {
            // Arrange
            var request = new TranslationRequest
            {
                Items = new List<TranslationItem>
                {
                    new TranslationItem { Text = "Hello", To = "es" },
                    new TranslationItem { Text = "World", To = "fr" }
                }
            };
            
            // Act
            // Serializar a XML
            var serializer = new XmlSerializer(typeof(TranslationRequest));
            using var stream = new MemoryStream();
            serializer.Serialize(stream, request);
            stream.Position = 0;
            
            // Deserializar de vuelta
            var deserializedRequest = (TranslationRequest)serializer.Deserialize(stream);
            
            // Assert
            Assert.Equal(2, deserializedRequest.Items.Count);
            Assert.Equal("Hello", deserializedRequest.Items[0].Text);
            Assert.Equal("es", deserializedRequest.Items[0].To);
            Assert.Equal("World", deserializedRequest.Items[1].Text);
            Assert.Equal("fr", deserializedRequest.Items[1].To);
        }

        [Fact]
        public void TranslationResponse_XmlSerialization_DeserializesCorrectly()
        {
            // Arrange
            var response = new TranslationResponse
            {
                Results = new List<TranslationResult>
                {
                    new TranslationResult { Text = "Hello", TranslatedText = "Hola", To = "es" },
                    new TranslationResult { Text = "World", TranslatedText = "Monde", To = "fr" }
                }
            };
            
            // Act
            // Serializar a XML
            var serializer = new XmlSerializer(typeof(TranslationResponse));
            using var stream = new MemoryStream();
            serializer.Serialize(stream, response);
            stream.Position = 0;
            
            // Deserializar de vuelta
            var deserializedResponse = (TranslationResponse)serializer.Deserialize(stream);
            
            // Assert
            Assert.Equal(2, deserializedResponse.Results.Count);
            Assert.Equal("Hello", deserializedResponse.Results[0].Text);
            Assert.Equal("Hola", deserializedResponse.Results[0].TranslatedText);
            Assert.Equal("es", deserializedResponse.Results[0].To);
            Assert.Equal("World", deserializedResponse.Results[1].Text);
            Assert.Equal("Monde", deserializedResponse.Results[1].TranslatedText);
            Assert.Equal("fr", deserializedResponse.Results[1].To);
        }

        [Fact]
        public void TranslationRequest_WithEmptyItems_SerializesCorrectly()
        {
            // Arrange
            var request = new TranslationRequest();
            
            // Act
            var serializer = new XmlSerializer(typeof(TranslationRequest));
            using var stream = new MemoryStream();
            
            // Assert - No debería lanzar excepción
            var exception = Record.Exception(() => serializer.Serialize(stream, request));
            Assert.Null(exception);
            
            // Verificar que la lista de items se inicializa automáticamente
            Assert.NotNull(request.Items);
            Assert.Empty(request.Items);
        }
    }
}
