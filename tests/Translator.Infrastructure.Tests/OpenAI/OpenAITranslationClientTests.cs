using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Contrib.HttpClient;
using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Translator.Infrastructure.OpenAI;
using Xunit;

namespace Translator.Infrastructure.Tests.OpenAI
{
    public class OpenAITranslationClientTests : IDisposable
    {
        private readonly Mock<HttpMessageHandler> _mockHttpHandler;
        private readonly Mock<ILogger<OpenAITranslationClient>> _mockLogger;
        private readonly Mock<IOptions<OpenAIOptions>> _mockOptions;
        private readonly HttpClient _httpClient;
        private readonly OpenAITranslationClient _client;

        public OpenAITranslationClientTests()
        {
            _mockHttpHandler = new Mock<HttpMessageHandler>();
            _mockLogger = new Mock<ILogger<OpenAITranslationClient>>();
            _mockOptions = new Mock<IOptions<OpenAIOptions>>();

            _httpClient = new HttpClient(_mockHttpHandler.Object);
            
            _mockOptions.Setup(x => x.Value).Returns(new OpenAIOptions
            {
                ApiKey = "test-api-key",
                ModelName = "gpt-3.5-turbo"
            });

            _client = new OpenAITranslationClient(_httpClient, _mockOptions.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task TranslateTextAsync_WithSuccessfulResponse_ReturnsTranslation()
        {
            // Arrange
            var text = "Hello";
            var targetLanguage = "es";
            var expectedTranslation = "Hola";

            var openAIResponse = new
            {
                choices = new[]
                {
                    new
                    {
                        message = new
                        {
                            content = expectedTranslation
                        }
                    }
                }
            };

            var responseJson = JsonSerializer.Serialize(openAIResponse);
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson)
            };

            _mockHttpHandler.SetupAnyRequest()
                .ReturnsAsync(httpResponse);

            // Act
            var result = await _client.TranslateTextAsync(text, targetLanguage);

            // Assert
            Assert.Equal(expectedTranslation, result);
            
            // Verify HTTP request was made
            _mockHttpHandler.VerifyAnyRequest(Times.Once());
        }

        [Fact]
        public async Task TranslateTextAsync_WithQuotedResponse_RemovesQuotes()
        {
            // Arrange
            var text = "Hello";
            var targetLanguage = "es";
            var quotedTranslation = "\"Hola\"";
            var expectedTranslation = "Hola";

            var openAIResponse = new
            {
                choices = new[]
                {
                    new
                    {
                        message = new
                        {
                            content = quotedTranslation
                        }
                    }
                }
            };

            var responseJson = JsonSerializer.Serialize(openAIResponse);
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson)
            };

            _mockHttpHandler.SetupAnyRequest()
                .ReturnsAsync(httpResponse);

            // Act
            var result = await _client.TranslateTextAsync(text, targetLanguage);

            // Assert
            Assert.Equal(expectedTranslation, result);
        }

        [Fact]
        public async Task TranslateTextAsync_WithHttpError_ReturnsErrorMessage()
        {
            // Arrange
            var text = "Hello";
            var targetLanguage = "es";

            var httpResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("Bad Request")
            };

            _mockHttpHandler.SetupAnyRequest()
                .ReturnsAsync(httpResponse);

            // Act
            var result = await _client.TranslateTextAsync(text, targetLanguage);

            // Assert
            Assert.StartsWith("ERROR: Error en API de OpenAI", result);
        }

        [Fact]
        public async Task TranslateTextAsync_WithEmptyResponse_ReturnsErrorMessage()
        {
            // Arrange
            var text = "Hello";
            var targetLanguage = "es";

            var openAIResponse = new
            {
                choices = new[]
                {
                    new
                    {
                        message = new
                        {
                            content = ""
                        }
                    }
                }
            };

            var responseJson = JsonSerializer.Serialize(openAIResponse);
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson)
            };

            _mockHttpHandler.SetupAnyRequest()
                .ReturnsAsync(httpResponse);

            // Act
            var result = await _client.TranslateTextAsync(text, targetLanguage);

            // Assert
            Assert.Equal("ERROR: Se recibió una traducción vacía", result);
        }

        [Fact]
        public async Task TranslateTextAsync_WithMalformedJson_ReturnsEmptyTranslation()
        {
            // Arrange
            var text = "Hello";
            var targetLanguage = "es";

            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("invalid json")
            };

            _mockHttpHandler.SetupAnyRequest()
                .ReturnsAsync(httpResponse);

            // Act
            var result = await _client.TranslateTextAsync(text, targetLanguage);

            // Assert - The implementation catches JSON exceptions and returns an empty translation error
            Assert.Equal("ERROR: Se recibió una traducción vacía", result);
        }

        [Fact]
        public void Constructor_WithNullHttpClient_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new OpenAITranslationClient(null!, _mockOptions.Object, _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullOptions_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new OpenAITranslationClient(_httpClient, null!, _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new OpenAITranslationClient(_httpClient, _mockOptions.Object, null!));
        }

        [Fact]
        public void Constructor_SetsCorrectBaseAddressAndHeaders()
        {
            // Arrange - Use a fresh HttpClient to avoid header conflicts
            using var freshHttpClient = new HttpClient();
            var freshOptions = new Mock<IOptions<OpenAIOptions>>();
            freshOptions.Setup(x => x.Value).Returns(new OpenAIOptions
            {
                ApiKey = "fresh-test-key",
                ModelName = "gpt-3.5-turbo"
            });

            // Act
            var client = new OpenAITranslationClient(freshHttpClient, freshOptions.Object, _mockLogger.Object);

            // Assert
            Assert.Equal("https://api.openai.com/", freshHttpClient.BaseAddress!.ToString());
            Assert.Contains("Authorization", freshHttpClient.DefaultRequestHeaders.ToString());
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
} 