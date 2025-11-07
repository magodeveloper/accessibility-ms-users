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

        #region Register Tests

        [Fact]
        public async Task Register_ValidData_ReturnsCreatedWithUserAndPreferences()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Email = "newuser@example.com",
                Password = "SecurePass123",
                Name = "New",
                Lastname = "User",
                Nickname = "newuser"
            };

            _mockPasswordService.Setup(x => x.Hash(registerDto.Password))
                               .Returns("$2a$11$hashedpassword");

            // Act
            var result = await _controller.Register(registerDto);

            // Assert
            var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
            createdResult.StatusCode.Should().Be(201);

            var response = createdResult.Value.Should().BeOfType<UserWithPreferenceReadDto>().Subject;
            response.Email.Should().Be(registerDto.Email.ToLower());
            response.Name.Should().Be(registerDto.Name);
            response.Lastname.Should().Be(registerDto.Lastname);
            response.Nickname.Should().Be(registerDto.Nickname);
            response.Role.Should().Be("user");
            response.Status.Should().Be("active");
            response.EmailConfirmed.Should().BeFalse();
            response.Preference.Should().NotBeNull();
            response.Preference!.WcagLevel.Should().Be("AA");
            response.Preference.Language.Should().Be("es");

            // Verify user was created in database
            var userInDb = await _context.Users.Include(u => u.Preference)
                                               .FirstOrDefaultAsync(u => u.Email == registerDto.Email.ToLower());
            userInDb.Should().NotBeNull();
            userInDb!.Preference.Should().NotBeNull();
        }

        [Fact]
        public async Task Register_EmailAlreadyExists_ReturnsConflict()
        {
            // Arrange
            var existingUser = new User
            {
                Email = "existing@example.com",
                Nickname = "existing",
                Name = "Existing",
                Lastname = "User",
                Password = "hashedpass",
                Role = UserRole.user,
                Status = UserStatus.active,
                EmailConfirmed = true,
                RegistrationDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Users.Add(existingUser);
            await _context.SaveChangesAsync();

            var registerDto = new RegisterDto
            {
                Email = "existing@example.com",
                Password = "NewPass123",
                Name = "Another",
                Lastname = "User",
                Nickname = "another"
            };

            // Act
            var result = await _controller.Register(registerDto);

            // Assert
            var conflictResult = result.Should().BeOfType<ConflictObjectResult>().Subject;
            conflictResult.StatusCode.Should().Be(409);
        }

        [Fact]
        public async Task Register_EmailCaseInsensitive_ReturnsConflict()
        {
            // Arrange
            var existingUser = new User
            {
                Email = "test@example.com",
                Nickname = "test",
                Name = "Test",
                Lastname = "User",
                Password = "hashedpass",
                Role = UserRole.user,
                Status = UserStatus.active,
                EmailConfirmed = true,
                RegistrationDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Users.Add(existingUser);
            await _context.SaveChangesAsync();

            var registerDto = new RegisterDto
            {
                Email = "TEST@EXAMPLE.COM", // Different case
                Password = "Pass123",
                Name = "Another",
                Lastname = "User"
            };

            // Act
            var result = await _controller.Register(registerDto);

            // Assert
            result.Should().BeOfType<ConflictObjectResult>();
        }

        [Fact]
        public async Task Register_ShortPassword_ReturnsBadRequest()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Email = "user@example.com",
                Password = "123", // Too short
                Name = "Test",
                Lastname = "User"
            };

            // Act
            var result = await _controller.Register(registerDto);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task Register_WithoutNickname_UsesEmailPrefix()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Email = "johndoe@example.com",
                Password = "SecurePass123",
                Name = "John",
                Lastname = "Doe",
                Nickname = null // No nickname provided
            };

            _mockPasswordService.Setup(x => x.Hash(registerDto.Password))
                               .Returns("$2a$11$hashedpassword");

            // Act
            var result = await _controller.Register(registerDto);

            // Assert
            var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
            var response = createdResult.Value.Should().BeOfType<UserWithPreferenceReadDto>().Subject;
            response.Nickname.Should().Be("johndoe"); // Should use part before @
        }

        [Fact]
        public async Task Register_CreatesUserWithCorrectDefaults()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Email = "defaults@example.com",
                Password = "SecurePass123",
                Name = "Default",
                Lastname = "User",
                Nickname = "defaultuser"
            };

            _mockPasswordService.Setup(x => x.Hash(registerDto.Password))
                               .Returns("$2a$11$hashedpassword");

            // Act
            var result = await _controller.Register(registerDto);

            // Assert
            var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
            var response = createdResult.Value.Should().BeOfType<UserWithPreferenceReadDto>().Subject;

            // Check user defaults
            response.Role.Should().Be("user"); // Always user, never admin
            response.Status.Should().Be("active");
            response.EmailConfirmed.Should().BeFalse(); // Requires confirmation
            response.LastLogin.Should().BeNull();

            // Check preference defaults
            response.Preference.Should().NotBeNull();
            response.Preference!.WcagVersion.Should().Be("2.2");
            response.Preference.WcagLevel.Should().Be("AA");
            response.Preference.Language.Should().Be("es");
            response.Preference.VisualTheme.Should().Be("light");
            response.Preference.ReportFormat.Should().Be("pdf");
            response.Preference.NotificationsEnabled.Should().BeTrue();
            response.Preference.AiResponseLevel.Should().Be("detailed");
            response.Preference.FontSize.Should().Be(16);
        }

        [Fact]
        public async Task Register_HashesPassword()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Email = "secure@example.com",
                Password = "PlainTextPassword123",
                Name = "Secure",
                Lastname = "User",
                Nickname = "secure"
            };

            var hashedPassword = "$2a$11$hashedpasswordstring";
            _mockPasswordService.Setup(x => x.Hash(registerDto.Password))
                               .Returns(hashedPassword);

            // Act
            var result = await _controller.Register(registerDto);

            // Assert
            result.Should().BeOfType<CreatedAtActionResult>();

            // Verify password was hashed
            _mockPasswordService.Verify(x => x.Hash(registerDto.Password), Times.Once);

            var userInDb = await _context.Users.FirstOrDefaultAsync(u => u.Email == registerDto.Email.ToLower());
            userInDb.Should().NotBeNull();
            userInDb!.Password.Should().Be(hashedPassword);
            userInDb.Password.Should().NotBe(registerDto.Password); // Never store plain text
        }

        [Fact]
        public async Task Register_ConvertsEmailToLowerCase()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Email = "MixedCase@EXAMPLE.COM",
                Password = "SecurePass123",
                Name = "Mixed",
                Lastname = "Case",
                Nickname = "mixed"
            };

            _mockPasswordService.Setup(x => x.Hash(registerDto.Password))
                               .Returns("$2a$11$hashedpassword");

            // Act
            var result = await _controller.Register(registerDto);

            // Assert
            var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
            var response = createdResult.Value.Should().BeOfType<UserWithPreferenceReadDto>().Subject;
            response.Email.Should().Be("mixedcase@example.com"); // Stored in lowercase

            var userInDb = await _context.Users.FirstOrDefaultAsync(u => u.Email == "mixedcase@example.com");
            userInDb.Should().NotBeNull();
        }

        [Fact]
        public async Task Register_CreatesPreferenceWithUserId()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Email = "withpref@example.com",
                Password = "SecurePass123",
                Name = "With",
                Lastname = "Preference",
                Nickname = "withpref"
            };

            _mockPasswordService.Setup(x => x.Hash(registerDto.Password))
                               .Returns("$2a$11$hashedpassword");

            // Act
            var result = await _controller.Register(registerDto);

            // Assert
            var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
            var response = createdResult.Value.Should().BeOfType<UserWithPreferenceReadDto>().Subject;

            // Verify preference is linked to user
            response.Preference.Should().NotBeNull();
            response.Preference!.UserId.Should().Be(response.Id);

            var prefInDb = await _context.Preferences.FirstOrDefaultAsync(p => p.UserId == response.Id);
            prefInDb.Should().NotBeNull();
            prefInDb!.UserId.Should().Be(response.Id);
        }

        [Fact]
        public async Task Register_EmptyPassword_ReturnsBadRequest()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Email = "test@example.com",
                Password = "", // Empty
                Name = "Test",
                Lastname = "User"
            };

            // Act
            var result = await _controller.Register(registerDto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task Register_WhitespacePassword_ReturnsBadRequest()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Email = "test@example.com",
                Password = "     ", // Only whitespace
                Name = "Test",
                Lastname = "User"
            };

            // Act
            var result = await _controller.Register(registerDto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        #endregion

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