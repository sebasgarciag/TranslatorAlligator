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
    public class TranslationServiceTests
    {
        private readonly Mock<IAITranslationClient> _mockAiClient;
        private readonly Mock<ICacheProvider> _mockCache;
        private readonly Mock<IOptions<TranslationOptions>> _mockOptions;
        private readonly Mock<ILogger<TranslationService>> _mockLogger;
        private readonly TranslationService _service;

        public TranslationServiceTests()
        {
            _mockAiClient = new Mock<IAITranslationClient>();
            _mockCache = new Mock<ICacheProvider>();
            _mockOptions = new Mock<IOptions<TranslationOptions>>();
            _mockLogger = new Mock<ILogger<TranslationService>>();

            _mockOptions.Setup(x => x.Value).Returns(new TranslationOptions
            {
                CacheTTL = TimeSpan.FromHours(24)
            });

            _service = new TranslationService(
                _mockAiClient.Object,
                _mockCache.Object,
                _mockOptions.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task TranslateAsync_WithEmptyList_ReturnsEmptyList()
        {
            // Arrange
            var items = new List<TranslationItem>();

            // Act
            var result = await _service.TranslateAsync(items);

            // Assert
            Assert.Empty(result);
            _mockAiClient.Verify(x => x.TranslateTextAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _mockCache.Verify(x => x.GetAsync<string>(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task TranslateAsync_WithCacheHit_ReturnsCachedTranslation()
        {
            // Arrange
            var items = new List<TranslationItem>
            {
                new TranslationItem { Text = "Hello", To = "es" }
            };

            var cachedTranslation = "Hola";
            var cacheKey = "Hello|es";

            _mockCache.Setup(x => x.GetAsync<string>(cacheKey))
                .ReturnsAsync(cachedTranslation);

            // Act
            var result = await _service.TranslateAsync(items);

            // Assert
            Assert.Single(result);
            Assert.Equal("Hello", result[0].Text);
            Assert.Equal("es", result[0].To);
            Assert.Equal(cachedTranslation, result[0].TranslatedText);

            _mockCache.Verify(x => x.GetAsync<string>(cacheKey), Times.Once);
            _mockAiClient.Verify(x => x.TranslateTextAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _mockCache.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan?>()), Times.Never);
        }

        [Fact]
        public async Task TranslateAsync_WithCacheMiss_CallsAIClientAndCachesResult()
        {
            // Arrange
            var items = new List<TranslationItem>
            {
                new TranslationItem { Text = "Hello", To = "es" }
            };

            var aiTranslation = "Hola";
            var cacheKey = "Hello|es";

            _mockCache.Setup(x => x.GetAsync<string>(cacheKey))
                .ReturnsAsync((string?)null);

            _mockAiClient.Setup(x => x.TranslateTextAsync("Hello", "es"))
                .ReturnsAsync(aiTranslation);

            // Act
            var result = await _service.TranslateAsync(items);

            // Assert
            Assert.Single(result);
            Assert.Equal("Hello", result[0].Text);
            Assert.Equal("es", result[0].To);
            Assert.Equal(aiTranslation, result[0].TranslatedText);

            _mockCache.Verify(x => x.GetAsync<string>(cacheKey), Times.Once);
            _mockAiClient.Verify(x => x.TranslateTextAsync("Hello", "es"), Times.Once);
            _mockCache.Verify(x => x.SetAsync(cacheKey, aiTranslation, It.IsAny<TimeSpan?>()), Times.Once);
        }
    }
} 