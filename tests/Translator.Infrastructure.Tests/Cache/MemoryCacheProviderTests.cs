using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading.Tasks;
using Translator.Infrastructure.Cache;
using Xunit;

namespace Translator.Infrastructure.Tests.Cache
{
    public class MemoryCacheProviderTests
    {
        private readonly Mock<IMemoryCache> _mockCache;
        private readonly Mock<ILogger<MemoryCacheProvider>> _mockLogger;
        private readonly MemoryCacheProvider _cacheProvider;

        public MemoryCacheProviderTests()
        {
            _mockCache = new Mock<IMemoryCache>();
            _mockLogger = new Mock<ILogger<MemoryCacheProvider>>();
            _cacheProvider = new MemoryCacheProvider(_mockCache.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GetAsync_WithExistingKey_ReturnsValue()
        {
            // Arrange
            var key = "test-key";
            var expectedValue = "test-value";
            object? cacheValue = expectedValue;

            _mockCache.Setup(x => x.TryGetValue(key, out cacheValue))
                .Returns(true);

            // Act
            var result = await _cacheProvider.GetAsync<string>(key);

            // Assert
            Assert.Equal(expectedValue, result);
            _mockCache.Verify(x => x.TryGetValue(key, out cacheValue), Times.Once);
        }

        [Fact]
        public async Task GetAsync_WithNonExistingKey_ReturnsDefault()
        {
            // Arrange
            var key = "non-existing-key";
            object? cacheValue = null;

            _mockCache.Setup(x => x.TryGetValue(key, out cacheValue))
                .Returns(false);

            // Act
            var result = await _cacheProvider.GetAsync<string>(key);

            // Assert
            Assert.Null(result);
            _mockCache.Verify(x => x.TryGetValue(key, out cacheValue), Times.Once);
        }

        [Fact]
        public async Task GetAsync_WithException_ReturnsDefaultAndLogsError()
        {
            // Arrange
            var key = "error-key";
            object? cacheValue;

            _mockCache.Setup(x => x.TryGetValue(key, out cacheValue))
                .Throws(new InvalidOperationException("Cache error"));

            // Act
            var result = await _cacheProvider.GetAsync<string>(key);

            // Assert
            Assert.Null(result);
            
            // Verify error was logged
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error al recuperar valor de caché para clave: error-key")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task SetAsync_WithValue_CallsCacheSet()
        {
            // Arrange
            var key = "test-key";
            var value = "test-value";
            var expiry = TimeSpan.FromHours(1);

            var mockEntry = new Mock<ICacheEntry>();
            _mockCache.Setup(x => x.CreateEntry(key))
                .Returns(mockEntry.Object);

            // Act
            await _cacheProvider.SetAsync(key, value, expiry);

            // Assert
            _mockCache.Verify(x => x.CreateEntry(key), Times.Once);
            mockEntry.VerifySet(x => x.Value = value, Times.Once);
            mockEntry.VerifySet(x => x.AbsoluteExpirationRelativeToNow = expiry, Times.Once);
        }

        [Fact]
        public async Task SetAsync_WithoutExpiry_UsesDefaultTTL()
        {
            // Arrange
            var key = "test-key";
            var value = "test-value";

            var mockEntry = new Mock<ICacheEntry>();
            _mockCache.Setup(x => x.CreateEntry(key))
                .Returns(mockEntry.Object);

            // Act
            await _cacheProvider.SetAsync(key, value);

            // Assert
            _mockCache.Verify(x => x.CreateEntry(key), Times.Once);
            mockEntry.VerifySet(x => x.Value = value, Times.Once);
            mockEntry.VerifySet(x => x.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24), Times.Once);
        }

        [Fact]
        public async Task SetAsync_WithException_LogsErrorAndCompletes()
        {
            // Arrange
            var key = "error-key";
            var value = "test-value";

            _mockCache.Setup(x => x.CreateEntry(key))
                .Throws(new InvalidOperationException("Cache set error"));

            // Act & Assert - Should not throw
            await _cacheProvider.SetAsync(key, value);

            // Verify error was logged
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error al establecer valor en caché para clave: error-key")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void Constructor_WithNullCache_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new MemoryCacheProvider(null!, _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new MemoryCacheProvider(_mockCache.Object, null!));
        }
    }
} 