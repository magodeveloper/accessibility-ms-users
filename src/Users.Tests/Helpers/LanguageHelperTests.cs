using Moq;
using Xunit;
using Users.Api.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Users.Tests.Helpers
{
    public class LanguageHelperTests
    {
        [Fact]
        public void GetRequestLanguage_WhenAcceptLanguageHeaderIsNull_ReturnsEs()
        {
            // Arrange
            var mockRequest = new Mock<HttpRequest>();
            var mockHeaders = new Mock<IHeaderDictionary>();

            mockHeaders.Setup(h => h["Accept-Language"]).Returns(new StringValues((string?)null));
            mockRequest.Setup(r => r.Headers).Returns(mockHeaders.Object);

            // Act
            var result = LanguageHelper.GetRequestLanguage(mockRequest.Object);

            // Assert
            Assert.Equal("es", result);
        }

        [Fact]
        public void GetRequestLanguage_WhenAcceptLanguageHeaderIsEmpty_ReturnsEs()
        {
            // Arrange
            var mockRequest = new Mock<HttpRequest>();
            var mockHeaders = new Mock<IHeaderDictionary>();

            mockHeaders.Setup(h => h["Accept-Language"]).Returns(new StringValues(""));
            mockRequest.Setup(r => r.Headers).Returns(mockHeaders.Object);

            // Act
            var result = LanguageHelper.GetRequestLanguage(mockRequest.Object);

            // Assert
            Assert.Equal("es", result);
        }

        [Fact]
        public void GetRequestLanguage_WhenAcceptLanguageHeaderIsWhitespace_ReturnsEs()
        {
            // Arrange
            var mockRequest = new Mock<HttpRequest>();
            var mockHeaders = new Mock<IHeaderDictionary>();

            mockHeaders.Setup(h => h["Accept-Language"]).Returns(new StringValues("   "));
            mockRequest.Setup(r => r.Headers).Returns(mockHeaders.Object);

            // Act
            var result = LanguageHelper.GetRequestLanguage(mockRequest.Object);

            // Assert
            Assert.Equal("es", result);
        }

        [Fact]
        public void GetRequestLanguage_WhenTokenLengthIsLessThan2_ReturnsEs()
        {
            // Arrange
            var mockRequest = new Mock<HttpRequest>();
            var mockHeaders = new Mock<IHeaderDictionary>();

            mockHeaders.Setup(h => h["Accept-Language"]).Returns(new StringValues("a"));
            mockRequest.Setup(r => r.Headers).Returns(mockHeaders.Object);

            // Act
            var result = LanguageHelper.GetRequestLanguage(mockRequest.Object);

            // Assert
            Assert.Equal("es", result);
        }

        [Fact]
        public void GetRequestLanguage_WhenLanguageIsEs_ReturnsEs()
        {
            // Arrange
            var mockRequest = new Mock<HttpRequest>();
            var mockHeaders = new Mock<IHeaderDictionary>();

            mockHeaders.Setup(h => h["Accept-Language"]).Returns(new StringValues("es-ES,es;q=0.9"));
            mockRequest.Setup(r => r.Headers).Returns(mockHeaders.Object);

            // Act
            var result = LanguageHelper.GetRequestLanguage(mockRequest.Object);

            // Assert
            Assert.Equal("es", result);
        }

        [Fact]
        public void GetRequestLanguage_WhenLanguageIsEn_ReturnsEn()
        {
            // Arrange
            var mockRequest = new Mock<HttpRequest>();
            var mockHeaders = new Mock<IHeaderDictionary>();

            mockHeaders.Setup(h => h["Accept-Language"]).Returns(new StringValues("en-US,en;q=0.8"));
            mockRequest.Setup(r => r.Headers).Returns(mockHeaders.Object);

            // Act
            var result = LanguageHelper.GetRequestLanguage(mockRequest.Object);

            // Assert
            Assert.Equal("en", result);
        }

        [Fact]
        public void GetRequestLanguage_WhenLanguageIsUnsupported_ReturnsEs()
        {
            // Arrange
            var mockRequest = new Mock<HttpRequest>();
            var mockHeaders = new Mock<IHeaderDictionary>();

            mockHeaders.Setup(h => h["Accept-Language"]).Returns(new StringValues("fr-FR,fr;q=0.9"));
            mockRequest.Setup(r => r.Headers).Returns(mockHeaders.Object);

            // Act
            var result = LanguageHelper.GetRequestLanguage(mockRequest.Object);

            // Assert
            Assert.Equal("es", result);
        }

        [Fact]
        public void GetRequestLanguage_WhenLanguageIsDe_ReturnsEs()
        {
            // Arrange
            var mockRequest = new Mock<HttpRequest>();
            var mockHeaders = new Mock<IHeaderDictionary>();

            mockHeaders.Setup(h => h["Accept-Language"]).Returns(new StringValues("de-DE"));
            mockRequest.Setup(r => r.Headers).Returns(mockHeaders.Object);

            // Act
            var result = LanguageHelper.GetRequestLanguage(mockRequest.Object);

            // Assert
            Assert.Equal("es", result);
        }

        [Fact]
        public void GetRequestLanguage_WhenExceptionOccurs_ReturnsEs()
        {
            // Arrange
            var mockRequest = new Mock<HttpRequest>();
            var mockHeaders = new Mock<IHeaderDictionary>();

            // Simulamos una excepción al acceder a los headers
            mockHeaders.Setup(h => h["Accept-Language"]).Throws(new Exception("Simulated exception"));
            mockRequest.Setup(r => r.Headers).Returns(mockHeaders.Object);

            // Act
            var result = LanguageHelper.GetRequestLanguage(mockRequest.Object);

            // Assert
            Assert.Equal("es", result);
        }

        [Fact]
        public void GetRequestLanguage_WhenHeaderAccessThrowsException_ReturnsEs()
        {
            // Arrange
            var mockRequest = new Mock<HttpRequest>();

            // Simulamos una excepción más directa al acceder a headers
            mockRequest.Setup(r => r.Headers).Throws(new InvalidOperationException("Headers access failed"));

            // Act
            var result = LanguageHelper.GetRequestLanguage(mockRequest.Object);

            // Assert
            Assert.Equal("es", result);
        }

        [Theory]
        [InlineData("EN-US")]
        [InlineData("ES-ES")]
        [InlineData("en-uk")]
        [InlineData("es-mx")]
        public void GetRequestLanguage_HandlesVariousCasingCorrectly(string languageHeader)
        {
            // Arrange
            var mockRequest = new Mock<HttpRequest>();
            var mockHeaders = new Mock<IHeaderDictionary>();

            mockHeaders.Setup(h => h["Accept-Language"]).Returns(new StringValues(languageHeader));
            mockRequest.Setup(r => r.Headers).Returns(mockHeaders.Object);

            // Act
            var result = LanguageHelper.GetRequestLanguage(mockRequest.Object);

            // Assert
            var expected = languageHeader.ToLowerInvariant().StartsWith("en") ? "en" : "es";
            Assert.Equal(expected, result);
        }
    }
}