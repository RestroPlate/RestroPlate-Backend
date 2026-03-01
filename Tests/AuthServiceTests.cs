using Microsoft.Extensions.Configuration;
using Moq;
using RestroPlate.Models;
using RestroPlate.Models.DTOs;
using RestroPlate.Models.Interfaces;
using RestroPlate.Services;

namespace Tests;

public class AuthServiceTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static IConfiguration BuildConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JWT:Secret"]   = "TestSecretKeyThatIsAtLeast32CharactersLongForTests!",
                ["JWT:Issuer"]   = "RestroPlate",
                ["JWT:Audience"] = "RestroPlate"
            })
            .Build();

    private static RegisterRequestDto ValidRegisterRequest(string email = "donor@test.com") => new()
    {
        Name     = "Test Donor",
        Email    = email,
        Password = "StrongPassword123!",
        Role     = "DONOR"
    };

    // ── Registration tests ─────────────────────────────────────────────────────

    [Fact]
    public async Task Register_HashesPassword_DoesNotStoreRawPassword()
    {
        // Arrange
        var mockRepo = new Mock<IUserRepository>();
        mockRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        mockRepo.Setup(r => r.RegisterAsync(It.IsAny<User>())).ReturnsAsync(1);

        var service = new AuthService(mockRepo.Object, BuildConfig());

        // Act
        await service.RegisterAsync(ValidRegisterRequest());

        // Assert: the stored PasswordHash must NOT equal the raw password
        mockRepo.Verify(r => r.RegisterAsync(
            It.Is<User>(u => u.PasswordHash != "StrongPassword123!" && u.PasswordHash.StartsWith("$2"))),
            Times.Once);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ThrowsInvalidOperationException()
    {
        // Arrange
        var existingUser = new User { Email = "donor@test.com" };
        var mockRepo = new Mock<IUserRepository>();
        mockRepo.Setup(r => r.GetByEmailAsync("donor@test.com")).ReturnsAsync(existingUser);

        var service = new AuthService(mockRepo.Object, BuildConfig());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RegisterAsync(ValidRegisterRequest()));
        Assert.Equal("email is already exists in the database", exception.Message);
    }

    [Fact]
    public async Task Register_WithInvalidRole_ThrowsArgumentException()
    {
        // Arrange
        var mockRepo = new Mock<IUserRepository>();
        var service  = new AuthService(mockRepo.Object, BuildConfig());

        var request = ValidRegisterRequest();
        request.Role = "INVALID_ROLE";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.RegisterAsync(request));
    }

    // ── Login tests ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_ValidCredentials_ReturnsAuthResponseWithJwtToken()
    {
        // Arrange
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("StrongPassword123!");
        var storedUser = new User
        {
            UserId       = 1,
            Email        = "donor@test.com",
            PasswordHash = passwordHash,
            UserType     = "DONOR"
        };

        var mockRepo = new Mock<IUserRepository>();
        mockRepo.Setup(r => r.GetByEmailAsync("donor@test.com")).ReturnsAsync(storedUser);

        var service = new AuthService(mockRepo.Object, BuildConfig());

        // Act
        var result = await service.LoginAsync(new LoginRequestDto
        {
            Email    = "donor@test.com",
            Password = "StrongPassword123!"
        });

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Token);
        Assert.Equal("donor@test.com", result.Email);
        Assert.Equal("DONOR", result.Role);
    }

    [Fact]
    public async Task Login_InvalidPassword_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword!");
        var storedUser = new User
        {
            UserId       = 1,
            Email        = "donor@test.com",
            PasswordHash = passwordHash,
            UserType     = "DONOR"
        };

        var mockRepo = new Mock<IUserRepository>();
        mockRepo.Setup(r => r.GetByEmailAsync("donor@test.com")).ReturnsAsync(storedUser);

        var service = new AuthService(mockRepo.Object, BuildConfig());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.LoginAsync(new LoginRequestDto
            {
                Email    = "donor@test.com",
                Password = "WrongPassword!"
            }));
    }

    [Fact]
    public async Task Login_UnknownEmail_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var mockRepo = new Mock<IUserRepository>();
        mockRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        var service = new AuthService(mockRepo.Object, BuildConfig());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.LoginAsync(new LoginRequestDto
            {
                Email    = "unknown@test.com",
                Password = "AnyPassword!"
            }));
    }

    // ── Profile tests ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetProfile_ValidUserId_ReturnsUserProfileDto()
    {
        // Arrange
        var storedUser = new User
        {
            UserId      = 42,
            Name        = "Test Donor",
            Email       = "donor@test.com",
            UserType    = "DONOR",
            PhoneNumber = "0771234567",
            Address     = "Colombo",
            CreatedAt   = DateTime.UtcNow
        };

        var mockRepo = new Mock<IUserRepository>();
        mockRepo.Setup(r => r.GetByIdAsync(42)).ReturnsAsync(storedUser);

        var service = new AuthService(mockRepo.Object, BuildConfig());

        // Act
        var profile = await service.GetProfileAsync(42);

        // Assert
        Assert.Equal(42,               profile.UserId);
        Assert.Equal("Test Donor",     profile.Name);
        Assert.Equal("donor@test.com", profile.Email);
        Assert.Equal("DONOR",          profile.Role);
    }

    [Fact]
    public async Task GetProfile_UnknownUserId_ThrowsKeyNotFoundException()
    {
        // Arrange
        var mockRepo = new Mock<IUserRepository>();
        mockRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((User?)null);

        var service = new AuthService(mockRepo.Object, BuildConfig());

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.GetProfileAsync(999));
    }
}
