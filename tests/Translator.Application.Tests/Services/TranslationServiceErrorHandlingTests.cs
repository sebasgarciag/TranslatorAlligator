using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Translator.Application.Services;
using Translator.Core.Interfaces;
using Translator.Core.Models;
using Xunit;

namespace Translator.Application.Tests.Services
{
    public class TranslationServiceErrorHandlingTests
    {
        private readonly Mock<IAITranslationClient> _mockAiClient;
        private readonly Mock<ICacheProvider> _mockCache;
        private readonly Mock<IOptions<TranslationOptions>> _mockOptions;
        private readonly Mock<ILogger<TranslationService>> _mockLogger;

        public TranslationServiceErrorHandlingTests()
        {
            _mockAiClient = new Mock<IAITranslationClient>();
            _mockCache = new Mock<ICacheProvider>();
            _mockOptions = new Mock<IOptions<TranslationOptions>>();
            _mockLogger = new Mock<ILogger<TranslationService>>();

            _mockOptions.Setup(x => x.Value).Returns(new TranslationOptions
            {
                CacheTTL = TimeSpan.FromHours(24)
            });
        }

        [Fact]
        public async Task TranslateAsync_WithAIClientException_ReturnsErrorResult()
        {
            // Arrange
            var service = new TranslationService(
                _mockAiClient.Object,
                _mockCache.Object,
                _mockOptions.Object,
                _mockLogger.Object);

            var items = new List<TranslationItem>
            {
                new TranslationItem { Text = "Hello", To = "es" }
            };

            var cacheKey = "Hello|es";

            _mockCache.Setup(x => x.GetAsync<string>(cacheKey))
                .ReturnsAsync((string?)null);

            _mockAiClient.Setup(x => x.TranslateTextAsync("Hello", "es"))
                .ThrowsAsync(new InvalidOperationException("AI service error"));

            // Act
            var result = await service.TranslateAsync(items);

            // Assert
            Assert.Single(result);
            Assert.Equal("Hello", result[0].Text);
            Assert.Equal("es", result[0].To);
            Assert.StartsWith("ERROR:", result[0].TranslatedText);
            Assert.Contains("AI service error", result[0].TranslatedText);
        }

        [Fact]
        public async Task TranslateAsync_WithCacheException_ContinuesWithAIClient()
        {
            // Arrange
            var service = new TranslationService(
                _mockAiClient.Object,
                _mockCache.Object,
                _mockOptions.Object,
                _mockLogger.Object);

            var items = new List<TranslationItem>
            {
                new TranslationItem { Text = "Hello", To = "es" }
            };

            var cacheKey = "Hello|es";
            var expectedTranslation = "Hola";

            _mockCache.Setup(x => x.GetAsync<string>(cacheKey))
                .ThrowsAsync(new InvalidOperationException("Cache error"));

            _mockAiClient.Setup(x => x.TranslateTextAsync("Hello", "es"))
                .ReturnsAsync(expectedTranslation);

            // Act
            var result = await service.TranslateAsync(items);

            // Assert
            Assert.Single(result);
            Assert.Equal("Hello", result[0].Text);
            Assert.Equal("es", result[0].To);
            Assert.StartsWith("ERROR:", result[0].TranslatedText);
        }

        [Fact]
        public async Task TranslateAsync_WithNullItems_ReturnsEmptyList()
        {
            // Arrange
            var service = new TranslationService(
                _mockAiClient.Object,
                _mockCache.Object,
                _mockOptions.Object,
                _mockLogger.Object);

            // Act
            var result = await service.TranslateAsync(null!);

            // Assert
            Assert.Empty(result);
            _mockCache.Verify(x => x.GetAsync<string>(It.IsAny<string>()), Times.Never);
            _mockAiClient.Verify(x => x.TranslateTextAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task TranslateAsync_WithEmptyTextAndLanguage_HandlesGracefully()
        {
            // Arrange
            var service = new TranslationService(
                _mockAiClient.Object,
                _mockCache.Object,
                _mockOptions.Object,
                _mockLogger.Object);

            var items = new List<TranslationItem>
            {
                new TranslationItem { Text = "", To = "" },
                new TranslationItem { Text = null!, To = null! }
            };

            _mockCache.Setup(x => x.GetAsync<string>(It.IsAny<string>()))
                .ReturnsAsync((string?)null);

            _mockAiClient.Setup(x => x.TranslateTextAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("translated");

            // Act
            var result = await service.TranslateAsync(items);

            // Assert
            Assert.Equal(2, result.Count);
            
            // Verify cache keys are normalized - both items will have the same normalized key "|"
            _mockCache.Verify(x => x.GetAsync<string>("|"), Times.Exactly(2));
        }

        [Fact]
        public async Task TranslateAsync_WithWhitespaceText_NormalizesCorrectly()
        {
            // Arrange
            var service = new TranslationService(
                _mockAiClient.Object,
                _mockCache.Object,
                _mockOptions.Object,
                _mockLogger.Object);

            var items = new List<TranslationItem>
            {
                new TranslationItem { Text = "  Hello  ", To = "  ES  " }
            };

            var normalizedCacheKey = "Hello|es";
            var expectedTranslation = "Hola";

            _mockCache.Setup(x => x.GetAsync<string>(normalizedCacheKey))
                .ReturnsAsync((string?)null);

            _mockAiClient.Setup(x => x.TranslateTextAsync("  Hello  ", "  ES  "))
                .ReturnsAsync(expectedTranslation);

            // Act
            var result = await service.TranslateAsync(items);

            // Assert
            Assert.Single(result);
            Assert.Equal("  Hello  ", result[0].Text); // Original text preserved
            Assert.Equal("  ES  ", result[0].To); // Original language preserved
            Assert.Equal(expectedTranslation, result[0].TranslatedText);

            // Verify normalized cache key was used
            _mockCache.Verify(x => x.GetAsync<string>(normalizedCacheKey), Times.Once);
        }

        [Fact]
        public async Task TranslateAsync_WithMultipleItemsAndMixedErrors_HandlesEachIndependently()
        {
            // Arrange
            var service = new TranslationService(
                _mockAiClient.Object,
                _mockCache.Object,
                _mockOptions.Object,
                _mockLogger.Object);

            var items = new List<TranslationItem>
            {
                new TranslationItem { Text = "Hello", To = "es" },
                new TranslationItem { Text = "World", To = "fr" },
                new TranslationItem { Text = "Error", To = "de" }
            };

            // Setup cache misses for all
            _mockCache.Setup(x => x.GetAsync<string>(It.IsAny<string>()))
                .ReturnsAsync((string?)null);

            // Setup AI client responses
            _mockAiClient.Setup(x => x.TranslateTextAsync("Hello", "es"))
                .ReturnsAsync("Hola");
            
            _mockAiClient.Setup(x => x.TranslateTextAsync("World", "fr"))
                .ReturnsAsync("Monde");
            
            _mockAiClient.Setup(x => x.TranslateTextAsync("Error", "de"))
                .ThrowsAsync(new Exception("Translation failed"));

            // Act
            var result = await service.TranslateAsync(items);

            // Assert
            Assert.Equal(3, result.Count);
            
            // First item should succeed
            Assert.Equal("Hello", result[0].Text);
            Assert.Equal("Hola", result[0].TranslatedText);
            
            // Second item should succeed
            Assert.Equal("World", result[1].Text);
            Assert.Equal("Monde", result[1].TranslatedText);
            
            // Third item should have error
            Assert.Equal("Error", result[2].Text);
            Assert.StartsWith("ERROR:", result[2].TranslatedText);
        }

        [Fact]
        public void Constructor_WithNullAIClient_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new TranslationService(null!, _mockCache.Object, _mockOptions.Object, _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullCache_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new TranslationService(_mockAiClient.Object, null!, _mockOptions.Object, _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullOptions_ThrowsNullReferenceException()
        {
            // Act & Assert
            Assert.Throws<NullReferenceException>(() => 
                new TranslationService(_mockAiClient.Object, _mockCache.Object, null!, _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new TranslationService(_mockAiClient.Object, _mockCache.Object, _mockOptions.Object, null!));
        }

        [Fact]
        public async Task TranslateAsync_LogsAppropriateMessages()
        {
            // Arrange
            var service = new TranslationService(
                _mockAiClient.Object,
                _mockCache.Object,
                _mockOptions.Object,
                _mockLogger.Object);

            var items = new List<TranslationItem>
            {
                new TranslationItem { Text = "Hello", To = "es" }
            };

            var cacheKey = "Hello|es";

            _mockCache.Setup(x => x.GetAsync<string>(cacheKey))
                .ReturnsAsync((string?)null);

            _mockAiClient.Setup(x => x.TranslateTextAsync("Hello", "es"))
                .ReturnsAsync("Hola");

            // Act
            await service.TranslateAsync(items);

            // Assert - Verify cache miss was logged
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Fallo en caché para clave")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
} 