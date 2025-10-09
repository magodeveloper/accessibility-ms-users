using Moq;
using Xunit;
using FluentAssertions;
using Users.Api.Controllers;
using Users.Domain.Entities;
using Users.Application.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Users.Infrastructure.Data;
using Users.Application.Services;
using Microsoft.EntityFrameworkCore;
using Users.Application.Services.User;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Users.Tests.Controllers
{
    public class AuthControllerTests : IDisposable
    {
        private readonly Mock<IPasswordService> _mockPasswordService;
        private readonly Mock<ISessionTokenService> _mockTokenService;
        private readonly Mock<IJwtTokenService> _mockJwtTokenService;
        private readonly Mock<IUserService> _mockUserService;
        private readonly UsersDbContext _context;
        private readonly AuthController _controller;
        private bool _disposed = false;

        public AuthControllerTests()
        {
            // Setup InMemory database
            var options = new DbContextOptionsBuilder<UsersDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            _context = new UsersDbContext(options);

            // Setup mocks
            _mockPasswordService = new Mock<IPasswordService>();
            _mockTokenService = new Mock<ISessionTokenService>();
            _mockJwtTokenService = new Mock<IJwtTokenService>();
            _mockUserService = new Mock<IUserService>();

            // Setup controller
            _controller = new AuthController(_context, _mockPasswordService.Object, _mockTokenService.Object, _mockJwtTokenService.Object, _mockUserService.Object);

            // Setup HttpContext for language helper
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Accept-Language"] = "en";
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        [Fact]
        public async Task Login_ValidCredentials_ReturnsOkWithToken()
        {
            // Arrange
            var loginDto = new LoginDto("test@example.com", "password123");

            var user = new User
            {
                Id = 1,
                Email = loginDto.Email,
                Nickname = "testuser",
                Name = "Test",
                Lastname = "User",
                Role = UserRole.user,
                Status = UserStatus.active,
                EmailConfirmed = true,
                RegistrationDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxIiwiZW1haWwiOiJ0ZXN0QGV4YW1wbGUuY29tIiwicm9sZSI6InVzZXIiLCJuYW1lIjoiVGVzdCBVc2VyIn0.test-signature";
            var tokenHash = "hashed-token";
            var expiresAt = DateTime.UtcNow.AddHours(24);

            _mockUserService.Setup(x => x.AuthenticateAsync(loginDto.Email, loginDto.Password))
                           .ReturnsAsync(user);
            _mockJwtTokenService.Setup(x => x.GenerateToken(user.Id, user.Email, user.Role.ToString(), It.IsAny<string>()))
                               .Returns(token);
            _mockJwtTokenService.Setup(x => x.GetTokenExpiration())
                               .Returns(expiresAt);
            _mockTokenService.Setup(x => x.HashToken(token))
                            .Returns(tokenHash);
            _mockTokenService.Setup(x => x.GenerateToken())
                            .Returns((token, tokenHash));

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);

            var response = okResult.Value.Should().BeOfType<LoginResponseDto>().Subject;
            response.Token.Should().Be(token);
            response.User.Should().NotBeNull();
            response.User.Email.Should().Be(user.Email);
        }

        [Fact]
        public async Task Login_InvalidCredentials_ReturnsUnauthorized()
        {
            // Arrange
            var loginDto = new LoginDto("test@example.com", "wrongpassword");

            _mockUserService.Setup(x => x.AuthenticateAsync(loginDto.Email, loginDto.Password))
                           .ReturnsAsync((User?)null);

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
            unauthorizedResult.StatusCode.Should().Be(401);
        }

        [Fact]
        public async Task Login_InactiveUser_ReturnsForbidden()
        {
            // Arrange
            var loginDto = new LoginDto("inactive@example.com", "password123");

            var inactiveUser = new User
            {
                Id = 1,
                Email = loginDto.Email,
                Nickname = "inactiveuser",
                Name = "Inactive",
                Lastname = "User",
                Role = UserRole.user,
                Status = UserStatus.inactive,
                EmailConfirmed = true,
                RegistrationDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _mockUserService.Setup(x => x.AuthenticateAsync(loginDto.Email, loginDto.Password))
                           .ReturnsAsync(inactiveUser);

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async Task Login_BlockedUser_ReturnsForbidden()
        {
            // Arrange
            var loginDto = new LoginDto("blocked@example.com", "password123");

            var blockedUser = new User
            {
                Id = 1,
                Email = loginDto.Email,
                Nickname = "blockeduser",
                Name = "Blocked",
                Lastname = "User",
                Role = UserRole.user,
                Status = UserStatus.blocked,
                EmailConfirmed = true,
                RegistrationDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _mockUserService.Setup(x => x.AuthenticateAsync(loginDto.Email, loginDto.Password))
                           .ReturnsAsync(blockedUser);

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async Task Login_UserWithPreference_ReturnsOkWithPreference()
        {
            // Arrange
            var loginDto = new LoginDto("test@example.com", "password123");

            var preference = new Preference
            {
                Id = 1,
                UserId = 1,
                WcagVersion = "2.1",
                WcagLevel = WcagLevel.AA,
                Language = Language.en,
                VisualTheme = VisualTheme.light,
                ReportFormat = ReportFormat.pdf,
                NotificationsEnabled = true,
                FontSize = 14,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var user = new User
            {
                Id = 1,
                Email = loginDto.Email,
                Nickname = "testuser",
                Name = "Test",
                Lastname = "User",
                Role = UserRole.user,
                Status = UserStatus.active,
                EmailConfirmed = true,
                RegistrationDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Preference = preference
            };

            var token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxIiwiZW1haWwiOiJ0ZXN0QGV4YW1wbGUuY29tIiwicm9sZSI6InVzZXIiLCJuYW1lIjoiVGVzdCBVc2VyIn0.test-signature";
            var tokenHash = "hashed-token";
            var expiresAt = DateTime.UtcNow.AddHours(24);

            _mockUserService.Setup(x => x.AuthenticateAsync(loginDto.Email, loginDto.Password))
                           .ReturnsAsync(user);
            _mockJwtTokenService.Setup(x => x.GenerateToken(user.Id, user.Email, user.Role.ToString(), It.IsAny<string>()))
                               .Returns(token);
            _mockJwtTokenService.Setup(x => x.GetTokenExpiration())
                               .Returns(expiresAt);
            _mockTokenService.Setup(x => x.HashToken(token))
                            .Returns(tokenHash);
            _mockTokenService.Setup(x => x.GenerateToken())
                            .Returns((token, tokenHash));

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);

            var response = okResult.Value.Should().BeOfType<LoginResponseDto>().Subject;
            response.User.Preference.Should().NotBeNull();
            response.User.Preference!.WcagVersion.Should().Be("2.1");
        }

        [Fact]
        public async Task Logout_ExistingUser_ReturnsOk()
        {
            // Arrange
            var user = new User
            {
                Id = 1,
                Email = "test@example.com",
                Nickname = "testuser",
                Name = "Test",
                Lastname = "User",
                Password = "hashed-password",
                LastLogin = DateTime.UtcNow
            };

            var session = new Session
            {
                Id = 1,
                UserId = user.Id,
                TokenHash = "token-hash",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            };

            _context.Users.Add(user);
            _context.Sessions.Add(session);
            await _context.SaveChangesAsync();

            var logoutDto = new LogoutDto
            {
                Email = user.Email
            };

            // Act
            var result = await _controller.Logout(logoutDto);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);

            // Verify user's LastLogin is null
            var updatedUser = await _context.Users.FindAsync(user.Id);
            updatedUser!.LastLogin.Should().BeNull();

            // Verify sessions are removed
            var userSessions = _context.Sessions.Where(s => s.UserId == user.Id);
            userSessions.Should().BeEmpty();
        }

        [Fact]
        public async Task Logout_NonExistingUser_ReturnsNotFound()
        {
            // Arrange
            var logoutDto = new LogoutDto
            {
                Email = "nonexistent@example.com"
            };

            // Act
            var result = await _controller.Logout(logoutDto);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFoundResult.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task ResetPassword_ExistingUser_ReturnsOk()
        {
            // Arrange
            var originalDate = DateTime.UtcNow.AddDays(-1);
            var user = new User
            {
                Id = 1,
                Email = "test@example.com",
                Nickname = "testuser",
                Name = "Test",
                Lastname = "User",
                Password = "old-hashed-password",
                UpdatedAt = originalDate
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var resetDto = new ResetPasswordDto("test@example.com", "NewPassword123!");

            var hashedPassword = "new-hashed-password";
            _mockPasswordService.Setup(x => x.Hash(resetDto.NewPassword))
                              .Returns(hashedPassword);

            // Act
            var result = await _controller.ResetPassword(resetDto);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);

            // Verify password was updated
            var updatedUser = await _context.Users.FindAsync(user.Id);
            updatedUser!.Password.Should().Be(hashedPassword);
            updatedUser.UpdatedAt.Should().BeAfter(originalDate);
        }

        [Fact]
        public async Task ResetPassword_NonExistingUser_ReturnsNotFound()
        {
            // Arrange
            var resetDto = new ResetPasswordDto("nonexistent@example.com", "NewPassword123!");

            // Act
            var result = await _controller.ResetPassword(resetDto);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFoundResult.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task ConfirmEmail_ExistingUser_ReturnsOk()
        {
            // Arrange
            var originalDate = DateTime.UtcNow.AddDays(-1);
            var user = new User
            {
                Id = 1,
                Email = "test@example.com",
                Nickname = "testuser",
                Name = "Test",
                Lastname = "User",
                Password = "hashed-password",
                EmailConfirmed = false,
                UpdatedAt = originalDate
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.ConfirmEmail(user.Id);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);

            // Verify email was confirmed
            var updatedUser = await _context.Users.FindAsync(user.Id);
            updatedUser!.EmailConfirmed.Should().BeTrue();
            updatedUser.UpdatedAt.Should().BeAfter(originalDate);
        }

        [Fact]
        public async Task ConfirmEmail_NonExistingUser_ReturnsNotFound()
        {
            // Arrange
            var userId = 999;

            // Act
            var result = await _controller.ConfirmEmail(userId);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFoundResult.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task Login_CreatesSessionInDatabase()
        {
            // Arrange
            var loginDto = new LoginDto("test@example.com", "password123");

            var user = new User
            {
                Id = 1,
                Email = loginDto.Email,
                Nickname = "testuser",
                Name = "Test",
                Lastname = "User",
                Role = UserRole.user,
                Status = UserStatus.active,
                EmailConfirmed = true,
                RegistrationDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxIiwiZW1haWwiOiJ0ZXN0QGV4YW1wbGUuY29tIiwicm9sZSI6InVzZXIiLCJuYW1lIjoiVGVzdCBVc2VyIn0.test-signature";
            var tokenHash = "hashed-token";
            var expiresAt = DateTime.UtcNow.AddHours(24);

            _mockUserService.Setup(x => x.AuthenticateAsync(loginDto.Email, loginDto.Password))
                           .ReturnsAsync(user);
            _mockJwtTokenService.Setup(x => x.GenerateToken(user.Id, user.Email, user.Role.ToString(), It.IsAny<string>()))
                               .Returns(token);
            _mockJwtTokenService.Setup(x => x.GetTokenExpiration())
                               .Returns(expiresAt);
            _mockTokenService.Setup(x => x.HashToken(token))
                            .Returns(tokenHash);
            _mockTokenService.Setup(x => x.GenerateToken())
                            .Returns((token, tokenHash));

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();

            // Verify session was created
            var session = await _context.Sessions.FirstOrDefaultAsync(s => s.UserId == user.Id);
            session.Should().NotBeNull();
            session!.TokenHash.Should().Be(tokenHash);
            session.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _context.Dispose();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}