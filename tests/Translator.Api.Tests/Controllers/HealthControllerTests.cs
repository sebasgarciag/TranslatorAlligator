using Microsoft.AspNetCore.Mvc;
using System;
using Translator.Api.Controllers;
using Xunit;

namespace Translator.Api.Tests.Controllers
{
    public class HealthControllerTests
    {
        private readonly HealthController _controller;

        public HealthControllerTests()
        {
            _controller = new HealthController();
        }

        [Fact]
        public void Get_ReturnsOkResult()
        {
            // Act
            var result = _controller.Get();

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public void Get_ReturnsHealthyStatus()
        {
            // Act
            var result = _controller.Get() as OkObjectResult;
            var healthResponse = result?.Value;

            // Assert
            Assert.NotNull(healthResponse);
            
            // Use reflection to check the anonymous object properties
            var statusProperty = healthResponse.GetType().GetProperty("Status");
            var timestampProperty = healthResponse.GetType().GetProperty("Timestamp");
            
            Assert.NotNull(statusProperty);
            Assert.NotNull(timestampProperty);
            
            var status = statusProperty.GetValue(healthResponse)?.ToString();
            var timestamp = timestampProperty.GetValue(healthResponse);
            
            Assert.Equal("Healthy", status);
            Assert.IsType<DateTime>(timestamp);
        }

        [Fact]
        public void Get_ReturnsCurrentTimestamp()
        {
            // Arrange
            var beforeCall = DateTime.UtcNow;

            // Act
            var result = _controller.Get() as OkObjectResult;
            var healthResponse = result?.Value;
            
            var afterCall = DateTime.UtcNow;

            // Assert
            Assert.NotNull(healthResponse);
            
            var timestampProperty = healthResponse.GetType().GetProperty("Timestamp");
            var timestamp = (DateTime)timestampProperty.GetValue(healthResponse);
            
            // Verify timestamp is within reasonable range (should be UTC)
            Assert.True(timestamp >= beforeCall);
            Assert.True(timestamp <= afterCall);
            Assert.Equal(DateTimeKind.Utc, timestamp.Kind);
        }

        [Fact]
        public void Get_MultipleCallsReturnDifferentTimestamps()
        {
            // Act
            var result1 = _controller.Get() as OkObjectResult;
            
            // Small delay to ensure different timestamps
            System.Threading.Thread.Sleep(1);
            
            var result2 = _controller.Get() as OkObjectResult;

            // Assert
            var timestamp1 = GetTimestampFromResult(result1);
            var timestamp2 = GetTimestampFromResult(result2);
            
            Assert.True(timestamp2 > timestamp1);
        }

        [Fact]
        public void Get_AlwaysReturnsStatus200()
        {
            // Act
            var result = _controller.Get() as OkObjectResult;

            // Assert
            Assert.Equal(200, result?.StatusCode);
        }

        private DateTime GetTimestampFromResult(OkObjectResult result)
        {
            var healthResponse = result?.Value;
            var timestampProperty = healthResponse?.GetType().GetProperty("Timestamp");
            return (DateTime)timestampProperty?.GetValue(healthResponse);
        }
    }
} 