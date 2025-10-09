using Moq;
using Xunit;
using FluentAssertions;
using System.Security.Claims;
using Users.Domain.Entities;
using Users.Application.Dtos;
using Users.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Users.Application.Services.User;
using Users.Application.Services.UserContext;

namespace Users.Tests.Controllers
{
    public class UserControllerTests
    {
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<IUserContext> _mockUserContext;
        private readonly UserController _controller;

        public UserControllerTests()
        {
            _mockUserService = new Mock<IUserService>();
            _mockUserContext = new Mock<IUserContext>();

            // Configurar mock IUserContext - usuario autenticado como admin
            _mockUserContext.Setup(x => x.IsAuthenticated).Returns(true);
            _mockUserContext.Setup(x => x.UserId).Returns(1);
            _mockUserContext.Setup(x => x.Email).Returns("test@example.com");
            _mockUserContext.Setup(x => x.Role).Returns("Admin");
            _mockUserContext.Setup(x => x.IsAdmin).Returns(true);
            _mockUserContext.Setup(x => x.UserName).Returns("TestUser");

            _controller = new UserController(_mockUserService.Object, _mockUserContext.Object);

            // Setup HttpContext for language helper
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Accept-Language"] = "en";
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        [Fact]
        public async Task GetByEmail_ExistingUser_ReturnsOkWithUser()
        {
            // Arrange
            var email = "test@example.com";
            var user = new User
            {
                Id = 1,
                Email = email,
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

            _mockUserService.Setup(x => x.GetAllUsersAsync())
                           .ReturnsAsync(new List<User> { user });

            // Act
            var result = await _controller.GetByEmail(email);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);

            // Check the response structure without dynamic typing
            okResult.Value.Should().NotBeNull();
            var valueType = okResult.Value!.GetType();
            valueType.Should().NotBeNull();
        }

        [Fact]
        public async Task GetByEmail_NonExistingUser_ReturnsNotFound()
        {
            // Arrange
            var email = "nonexistent@example.com";
            _mockUserService.Setup(x => x.GetAllUsersAsync())
                           .ReturnsAsync(new List<User>());

            // Act
            var result = await _controller.GetByEmail(email);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFoundResult.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task DeleteByEmail_ExistingUser_ReturnsOk()
        {
            // Arrange
            var email = "test@example.com";
            var user = new User
            {
                Id = 1,
                Email = email,
                Nickname = "testuser",
                Name = "Test",
                Lastname = "User"
            };

            _mockUserService.Setup(x => x.GetAllUsersAsync())
                           .ReturnsAsync(new List<User> { user });
            _mockUserService.Setup(x => x.DeleteUserAsync(user.Id))
                           .ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteByEmail(email);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task DeleteByEmail_NonExistingUser_ReturnsNotFound()
        {
            // Arrange
            var email = "nonexistent@example.com";
            _mockUserService.Setup(x => x.GetAllUsersAsync())
                           .ReturnsAsync(new List<User>());

            // Act
            var result = await _controller.DeleteByEmail(email);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFoundResult.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task Create_ValidUser_ReturnsCreated()
        {
            // Arrange
            var dto = new UserCreateDto("newuser", "New", "User", "new@example.com", "Password123!");

            var createdUser = new User
            {
                Id = 1,
                Nickname = dto.Nickname,
                Name = dto.Name,
                Lastname = dto.Lastname,
                Email = dto.Email
            };

            _mockUserService.Setup(x => x.CreateUserAsync(It.IsAny<User>()))
                           .ReturnsAsync(createdUser);

            // Act
            var result = await _controller.Create(dto);

            // Assert
            var createdResult = result.Should().BeOfType<CreatedResult>().Subject;
            createdResult.StatusCode.Should().Be(201);
            createdResult.Location.Should().Be($"/api/user/{createdUser.Id}");
        }

        [Fact]
        public async Task Create_DuplicateEmail_ReturnsConflict()
        {
            // Arrange
            var dto = new UserCreateDto("newuser", "New", "User", "existing@example.com", "Password123!");

            _mockUserService.Setup(x => x.CreateUserAsync(It.IsAny<User>()))
                           .ThrowsAsync(new InvalidOperationException("email already exists"));

            // Act
            var result = await _controller.Create(dto);

            // Assert
            var conflictResult = result.Should().BeOfType<ConflictObjectResult>().Subject;
            conflictResult.StatusCode.Should().Be(409);
        }

        [Fact]
        public async Task Create_DuplicateNickname_ReturnsConflict()
        {
            // Arrange
            var dto = new UserCreateDto("existinguser", "New", "User", "new@example.com", "Password123!");

            _mockUserService.Setup(x => x.CreateUserAsync(It.IsAny<User>()))
                           .ThrowsAsync(new InvalidOperationException("nickname already exists"));

            // Act
            var result = await _controller.Create(dto);

            // Assert
            var conflictResult = result.Should().BeOfType<ConflictObjectResult>().Subject;
            conflictResult.StatusCode.Should().Be(409);
        }

        [Fact]
        public async Task GetById_ExistingUser_ReturnsOkWithUser()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Nickname = "testuser",
                Name = "Test",
                Lastname = "User",
                Email = "test@example.com",
                Role = UserRole.user,
                Status = UserStatus.active,
                EmailConfirmed = true,
                RegistrationDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _mockUserService.Setup(x => x.GetUserByIdAsync(userId))
                           .ReturnsAsync(user);

            // Act
            var result = await _controller.GetById(userId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task GetById_NonExistingUser_ReturnsNotFound()
        {
            // Arrange
            var userId = 999;
            _mockUserService.Setup(x => x.GetUserByIdAsync(userId))
                           .ReturnsAsync((User?)null);

            // Act
            var result = await _controller.GetById(userId);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFoundResult.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task GetAll_ReturnsOkWithUsersList()
        {
            // Arrange
            var users = new List<User>
            {
                new User
                {
                    Id = 1,
                    Nickname = "user1",
                    Name = "User",
                    Lastname = "One",
                    Email = "user1@example.com",
                    Role = UserRole.user,
                    Status = UserStatus.active,
                    EmailConfirmed = true,
                    RegistrationDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = 2,
                    Nickname = "user2",
                    Name = "User",
                    Lastname = "Two",
                    Email = "user2@example.com",
                    Role = UserRole.admin,
                    Status = UserStatus.active,
                    EmailConfirmed = false,
                    RegistrationDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            _mockUserService.Setup(x => x.GetAllUsersAsync())
                           .ReturnsAsync(users);

            // Act
            var result = await _controller.GetAll();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task Delete_ExistingUser_ReturnsOk()
        {
            // Arrange
            var userId = 1;
            _mockUserService.Setup(x => x.DeleteUserAsync(userId))
                           .ReturnsAsync(true);

            // Act
            var result = await _controller.Delete(userId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task Delete_NonExistingUser_ReturnsNotFound()
        {
            // Arrange
            var userId = 999;
            _mockUserService.Setup(x => x.DeleteUserAsync(userId))
                           .ReturnsAsync(false);

            // Act
            var result = await _controller.Delete(userId);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFoundResult.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task Update_ValidPatch_ReturnsOkWithUpdatedUser()
        {
            // Arrange
            var userId = 1;
            var patchDto = new UserPatchDto(null, "UpdatedName", "UpdatedLastname", null, null, null, null, null);

            var updatedUser = new User
            {
                Id = userId,
                Nickname = "testuser",
                Name = "UpdatedName",
                Lastname = "UpdatedLastname",
                Email = "test@example.com",
                Role = UserRole.user,
                Status = UserStatus.active,
                EmailConfirmed = true,
                RegistrationDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _mockUserService.Setup(x => x.UpdateUserAsync(userId, patchDto))
                           .ReturnsAsync(updatedUser);

            // Act
            var result = await _controller.Update(userId, patchDto);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task Update_NonExistingUser_ReturnsNotFound()
        {
            // Arrange
            var userId = 999;
            var patchDto = new UserPatchDto(null, "UpdatedName", null, null, null, null, null, null);

            _mockUserService.Setup(x => x.UpdateUserAsync(userId, patchDto))
                           .ReturnsAsync((User?)null);

            // Act
            var result = await _controller.Update(userId, patchDto);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFoundResult.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task Update_DuplicateEmail_ReturnsConflict()
        {
            // Arrange
            var userId = 1;
            var patchDto = new UserPatchDto(null, null, null, null, null, null, "existing@example.com", null);

            _mockUserService.Setup(x => x.UpdateUserAsync(userId, patchDto))
                           .ThrowsAsync(new InvalidOperationException("email already exists"));

            // Act
            var result = await _controller.Update(userId, patchDto);

            // Assert
            var conflictResult = result.Should().BeOfType<ConflictObjectResult>().Subject;
            conflictResult.StatusCode.Should().Be(409);
        }

        [Fact]
        public async Task Update_DuplicateNickname_ReturnsConflict()
        {
            // Arrange
            var userId = 1;
            var patchDto = new UserPatchDto("existinguser", null, null, null, null, null, null, null);

            _mockUserService.Setup(x => x.UpdateUserAsync(userId, patchDto))
                           .ThrowsAsync(new InvalidOperationException("nickname already exists"));

            // Act
            var result = await _controller.Update(userId, patchDto);

            // Assert
            var conflictResult = result.Should().BeOfType<ConflictObjectResult>().Subject;
            conflictResult.StatusCode.Should().Be(409);
        }

        [Fact]
        public async Task DeleteAllData_Success_ReturnsOk()
        {
            // Arrange
            _mockUserService.Setup(x => x.DeleteAllDataAsync())
                           .ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteAllData();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task DeleteAllData_Failure_ReturnsInternalServerError()
        {
            // Arrange
            _mockUserService.Setup(x => x.DeleteAllDataAsync())
                           .ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteAllData();

            // Assert
            var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(500);
        }

        // ===== Tests de autenticación =====

        [Fact]
        public async Task GetByEmail_NotAuthenticated_ReturnsUnauthorized()
        {
            // Arrange
            _mockUserContext.Setup(x => x.IsAuthenticated).Returns(false);
            var email = "test@example.com";

            // Act
            var result = await _controller.GetByEmail(email);

            // Assert
            var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
            unauthorizedResult.StatusCode.Should().Be(401);
        }

        [Fact]
        public async Task DeleteByEmail_NotAuthenticated_ReturnsUnauthorized()
        {
            // Arrange
            _mockUserContext.Setup(x => x.IsAuthenticated).Returns(false);
            var email = "test@example.com";

            // Act
            var result = await _controller.DeleteByEmail(email);

            // Assert
            var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
            unauthorizedResult.StatusCode.Should().Be(401);
        }

        [Fact]
        public async Task GetById_NotAuthenticated_ReturnsUnauthorized()
        {
            // Arrange
            _mockUserContext.Setup(x => x.IsAuthenticated).Returns(false);
            var userId = 1;

            // Act
            var result = await _controller.GetById(userId);

            // Assert
            var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
            unauthorizedResult.StatusCode.Should().Be(401);
        }

        [Fact]
        public async Task GetAll_NotAuthenticated_ReturnsUnauthorized()
        {
            // Arrange
            _mockUserContext.Setup(x => x.IsAuthenticated).Returns(false);

            // Act
            var result = await _controller.GetAll();

            // Assert
            var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
            unauthorizedResult.StatusCode.Should().Be(401);
        }

        [Fact]
        public async Task Delete_NotAuthenticated_ReturnsUnauthorized()
        {
            // Arrange
            _mockUserContext.Setup(x => x.IsAuthenticated).Returns(false);
            var userId = 1;

            // Act
            var result = await _controller.Delete(userId);

            // Assert
            var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
            unauthorizedResult.StatusCode.Should().Be(401);
        }

        [Fact]
        public async Task Update_NotAuthenticated_ReturnsUnauthorized()
        {
            // Arrange
            _mockUserContext.Setup(x => x.IsAuthenticated).Returns(false);
            var userId = 1;
            var dto = new UserPatchDto("updated", null, null, null, null, null, null, null);

            // Act
            var result = await _controller.Update(userId, dto);

            // Assert
            var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
            unauthorizedResult.StatusCode.Should().Be(401);
        }

        [Fact]
        public async Task DeleteAllData_NotAuthenticated_ReturnsUnauthorized()
        {
            // Arrange
            _mockUserContext.Setup(x => x.IsAuthenticated).Returns(false);

            // Act
            var result = await _controller.DeleteAllData();

            // Assert
            var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
            unauthorizedResult.StatusCode.Should().Be(401);
        }
    }
}