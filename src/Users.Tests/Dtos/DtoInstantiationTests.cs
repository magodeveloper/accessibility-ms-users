using Xunit;
using Users.Application.Dtos;

namespace Users.Tests.Dtos
{
    public class DtoInstantiationTests
    {
        [Fact]
        public void PreferencePatchDto_CanBeInstantiated()
        {
            // Arrange & Act
            var dto = new PreferencePatchDto(
                WcagVersion: "2.1",
                WcagLevel: "AA",
                Language: "es",
                VisualTheme: "light",
                ReportFormat: "pdf",
                NotificationsEnabled: true,
                AiResponseLevel: "intermediate",
                FontSize: 14
            );

            // Assert
            Assert.NotNull(dto);
            Assert.Equal("2.1", dto.WcagVersion);
            Assert.Equal("AA", dto.WcagLevel);
            Assert.Equal("es", dto.Language);
            Assert.Equal("light", dto.VisualTheme);
            Assert.Equal("pdf", dto.ReportFormat);
            Assert.True(dto.NotificationsEnabled);
            Assert.Equal("intermediate", dto.AiResponseLevel);
            Assert.Equal(14, dto.FontSize);
        }

        [Fact]
        public void PreferencePatchDto_CanBeInstantiatedWithNulls()
        {
            // Arrange & Act
            var dto = new PreferencePatchDto(
                WcagVersion: null,
                WcagLevel: null,
                Language: null,
                VisualTheme: null,
                ReportFormat: null,
                NotificationsEnabled: null,
                AiResponseLevel: null,
                FontSize: null
            );

            // Assert
            Assert.NotNull(dto);
            Assert.Null(dto.WcagVersion);
            Assert.Null(dto.WcagLevel);
            Assert.Null(dto.Language);
            Assert.Null(dto.VisualTheme);
            Assert.Null(dto.ReportFormat);
            Assert.Null(dto.NotificationsEnabled);
            Assert.Null(dto.AiResponseLevel);
            Assert.Null(dto.FontSize);
        }

        [Fact]
        public void ResetPasswordDto_CanBeInstantiated()
        {
            // Arrange & Act
            var dto = new ResetPasswordDto("test@example.com", "NewPassword123!");

            // Assert
            Assert.NotNull(dto);
            Assert.Equal("test@example.com", dto.Email);
            Assert.Equal("NewPassword123!", dto.NewPassword);
        }

        [Fact]
        public void ResetPasswordDto_CanBeInstantiatedWithDifferentEmails()
        {
            // Arrange & Act
            var dto1 = new ResetPasswordDto("user1@domain.com", "Pass1!");
            var dto2 = new ResetPasswordDto("user2@anotherdomain.org", "Pass2!");

            // Assert
            Assert.NotNull(dto1);
            Assert.NotNull(dto2);
            Assert.Equal("user1@domain.com", dto1.Email);
            Assert.Equal("user2@anotherdomain.org", dto2.Email);
        }
    }
}