using System.Collections.Generic;
using System.Text.Json;
using Translator.Core.Models;
using Xunit;

namespace Translator.Core.Tests
{
    public class JsonSerializationTests
    {
        private readonly JsonSerializerOptions _jsonOptions;

        public JsonSerializationTests()
        {
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
        }

        [Fact]
        public void TranslationRequest_JsonSerialization_SerializesAndDeserializesCorrectly()
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
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var deserializedRequest = JsonSerializer.Deserialize<TranslationRequest>(json, _jsonOptions);

            // Assert
            Assert.NotNull(deserializedRequest);
            Assert.Equal(2, deserializedRequest.Items.Count);
            Assert.Equal("Hello", deserializedRequest.Items[0].Text);
            Assert.Equal("es", deserializedRequest.Items[0].To);
            Assert.Equal("World", deserializedRequest.Items[1].Text);
            Assert.Equal("fr", deserializedRequest.Items[1].To);
        }

        [Fact]
        public void TranslationResponse_JsonSerialization_SerializesAndDeserializesCorrectly()
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
            var json = JsonSerializer.Serialize(response, _jsonOptions);
            var deserializedResponse = JsonSerializer.Deserialize<TranslationResponse>(json, _jsonOptions);

            // Assert
            Assert.NotNull(deserializedResponse);
            Assert.Equal(2, deserializedResponse.Results.Count);
            Assert.Equal("Hello", deserializedResponse.Results[0].Text);
            Assert.Equal("Hola", deserializedResponse.Results[0].TranslatedText);
            Assert.Equal("es", deserializedResponse.Results[0].To);
            Assert.Equal("World", deserializedResponse.Results[1].Text);
            Assert.Equal("Monde", deserializedResponse.Results[1].TranslatedText);
            Assert.Equal("fr", deserializedResponse.Results[1].To);
        }

        [Fact]
        public void TranslationRequest_WithEmptyItems_JsonSerializesCorrectly()
        {
            // Arrange
            var request = new TranslationRequest();

            // Act
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var deserializedRequest = JsonSerializer.Deserialize<TranslationRequest>(json, _jsonOptions);

            // Assert
            Assert.NotNull(deserializedRequest);
            Assert.NotNull(deserializedRequest.Items);
            Assert.Empty(deserializedRequest.Items);
        }

        [Fact]
        public void TranslationRequest_JsonFormat_UsesCamelCase()
        {
            // Arrange
            var request = new TranslationRequest
            {
                Items = new List<TranslationItem>
                {
                    new TranslationItem { Text = "Hello", To = "es" }
                }
            };

            // Act
            var json = JsonSerializer.Serialize(request, _jsonOptions);

            // Assert
            Assert.Contains("\"items\":", json);
            Assert.Contains("\"text\":", json);
            Assert.Contains("\"to\":", json);
            Assert.DoesNotContain("\"Items\":", json);
            Assert.DoesNotContain("\"Text\":", json);
            Assert.DoesNotContain("\"To\":", json);
        }

        [Fact]
        public void TranslationResponse_JsonFormat_UsesCamelCase()
        {
            // Arrange
            var response = new TranslationResponse
            {
                Results = new List<TranslationResult>
                {
                    new TranslationResult { Text = "Hello", TranslatedText = "Hola", To = "es" }
                }
            };

            // Act
            var json = JsonSerializer.Serialize(response, _jsonOptions);

            // Assert
            Assert.Contains("\"results\":", json);
            Assert.Contains("\"text\":", json);
            Assert.Contains("\"translatedText\":", json);
            Assert.Contains("\"to\":", json);
            Assert.DoesNotContain("\"Results\":", json);
            Assert.DoesNotContain("\"TranslatedText\":", json);
        }

        [Fact]
        public void TranslationItem_WithNullValues_JsonHandlesGracefully()
        {
            // Arrange
            var item = new TranslationItem { Text = null, To = null };

            // Act
            var json = JsonSerializer.Serialize(item, _jsonOptions);
            var deserializedItem = JsonSerializer.Deserialize<TranslationItem>(json, _jsonOptions);

            // Assert
            Assert.NotNull(deserializedItem);
            Assert.Null(deserializedItem.Text);
            Assert.Null(deserializedItem.To);
        }

        [Fact]
        public void TranslationResult_WithSpecialCharacters_JsonHandlesCorrectly()
        {
            // Arrange
            var result = new TranslationResult 
            { 
                Text = "Hello \"world\"", 
                TranslatedText = "Hola \"mundo\"", 
                To = "es" 
            };

            // Act
            var json = JsonSerializer.Serialize(result, _jsonOptions);
            var deserializedResult = JsonSerializer.Deserialize<TranslationResult>(json, _jsonOptions);

            // Assert
            Assert.NotNull(deserializedResult);
            Assert.Equal("Hello \"world\"", deserializedResult.Text);
            Assert.Equal("Hola \"mundo\"", deserializedResult.TranslatedText);
            Assert.Equal("es", deserializedResult.To);
        }

        [Fact]
        public void TranslationRequest_FromRealApiExample_DeserializesCorrectly()
        {
            // Arrange
            var json = @"{
                ""items"": [
                    {
                        ""text"": ""Hello world"",
                        ""to"": ""es""
                    },
                    {
                        ""text"": ""Good morning"",
                        ""to"": ""fr""
                    }
                ]
            }";

            // Act
            var request = JsonSerializer.Deserialize<TranslationRequest>(json, _jsonOptions);

            // Assert
            Assert.NotNull(request);
            Assert.Equal(2, request.Items.Count);
            Assert.Equal("Hello world", request.Items[0].Text);
            Assert.Equal("es", request.Items[0].To);
            Assert.Equal("Good morning", request.Items[1].Text);
            Assert.Equal("fr", request.Items[1].To);
        }
    }
} 