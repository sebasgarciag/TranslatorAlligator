using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Translator.Api.Controllers;
using Translator.Core.Interfaces;
using Translator.Core.Models;
using Xunit;

namespace Translator.Api.Tests.Controllers
{
    public class TranslateControllerTests
    {
        private readonly Mock<ITranslationService> _mockTranslationService;
        private readonly Mock<ILogger<TranslateController>> _mockLogger;
        private readonly TranslateController _controller;

        public TranslateControllerTests()
        {
            _mockTranslationService = new Mock<ITranslationService>();
            _mockLogger = new Mock<ILogger<TranslateController>>();
            _controller = new TranslateController(_mockTranslationService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task Translate_WithValidRequest_ReturnsOkResult()
        {
            // Arrange
            var request = new TranslationRequest
            {
                Items = new List<TranslationItem>
                {
                    new TranslationItem { Text = "Hello", To = "es" }
                }
            };

            var translationResults = new List<TranslationResult>
            {
                new TranslationResult { Text = "Hello", TranslatedText = "Hola", To = "es" }
            };

            _mockTranslationService.Setup(x => x.TranslateAsync(request.Items))
                .ReturnsAsync(translationResults);

            // Act
            var actionResult = await _controller.Translate(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult);
            var response = Assert.IsType<TranslationResponse>(okResult.Value);

            Assert.Single(response.Results);
            Assert.Equal("Hello", response.Results[0].Text);
            Assert.Equal("Hola", response.Results[0].TranslatedText);
            Assert.Equal("es", response.Results[0].To);

            _mockTranslationService.Verify(x => x.TranslateAsync(request.Items), Times.Once);
        }

        [Fact]
        public async Task Translate_WithEmptyRequest_ReturnsBadRequest()
        {
            // Arrange
            var request = new TranslationRequest
            {
                Items = new List<TranslationItem>()
            };

            // Act
            var actionResult = await _controller.Translate(request);

            // Assert
            Assert.IsType<BadRequestObjectResult>(actionResult);
            _mockTranslationService.Verify(x => x.TranslateAsync(It.IsAny<List<TranslationItem>>()), Times.Never);
        }

        [Fact]
        public async Task Translate_WithServiceError_ReturnsInternalServerError()
        {
            // Arrange
            var request = new TranslationRequest
            {
                Items = new List<TranslationItem>
                {
                    new TranslationItem { Text = "Hello", To = "es" }
                }
            };

            _mockTranslationService.Setup(x => x.TranslateAsync(request.Items))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var actionResult = await _controller.Translate(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(actionResult);
            Assert.Equal(500, statusCodeResult.StatusCode);

            _mockTranslationService.Verify(x => x.TranslateAsync(request.Items), Times.Once);
        }
    }
} 