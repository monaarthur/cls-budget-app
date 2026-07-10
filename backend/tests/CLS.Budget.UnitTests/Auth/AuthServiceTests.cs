using CLS.Budget.Application.Abstractions;
using CLS.Budget.Application.Abstractions.Repositories;
using CLS.Budget.Application.Abstractions.Services;
using CLS.Budget.Application.Auth;
using CLS.Budget.Application.Auth.Dtos;
using CLS.Budget.Domain.Entities;
using FluentAssertions;
using Moq;

namespace CLS.Budget.UnitTests.Auth;

public class AuthServiceTests
{
    private readonly Mock<IAppUserRepository> _userRepository = new();
    private readonly Mock<ITenantRepository> _tenantRepository = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly Mock<IPasswordResetTokenRepository> _passwordResetTokenRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<IPasswordResetNotifier> _passwordResetNotifier = new();
    private readonly Mock<IPasswordResetSettings> _passwordResetSettings = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _passwordResetSettings.Setup(s => s.TokenLifetime).Returns(TimeSpan.FromHours(1));

        _sut = new AuthService(
            _userRepository.Object,
            _tenantRepository.Object,
            _refreshTokenRepository.Object,
            _passwordResetTokenRepository.Object,
            _passwordHasher.Object,
            _tokenService.Object,
            _passwordResetNotifier.Object,
            _passwordResetSettings.Object);

        _tokenService.Setup(t => t.CreateAccessToken(It.IsAny<AppUser>()))
            .Returns(new AccessToken("access-token", DateTime.UtcNow.AddMinutes(15)));
        _tokenService.Setup(t => t.CreateRefreshToken())
            .Returns(new RefreshTokenResult("raw-refresh", "hash-refresh", DateTime.UtcNow.AddDays(30)));
        _tokenService.Setup(t => t.CreateOneTimeToken(It.IsAny<TimeSpan>()))
            .Returns(new RefreshTokenResult("raw-reset", "hash-reset", DateTime.UtcNow.AddHours(1)));
        _tokenService.Setup(t => t.HashRefreshToken("raw-refresh")).Returns("hash-refresh");
        _tokenService.Setup(t => t.HashRefreshToken("raw-reset")).Returns("hash-reset");
        _tokenService.Setup(t => t.HashRefreshToken("bad")).Returns("bad-hash");
    }

    [Fact]
    public async Task LoginAsync_ReturnsTokens_WhenCredentialsValid()
    {
        var user = SampleUser();
        _userRepository.Setup(r => r.GetByEmailAsync("user@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify(user.PasswordHash, "secret")).Returns(true);
        _refreshTokenRepository.Setup(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken token, CancellationToken _) => token);

        var result = await _sut.LoginAsync(
            new LoginRequest { Email = "user@example.com", Password = "secret" },
            CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.AccessToken.Should().Be("access-token");
        result.Data.RefreshToken.Should().Be("raw-refresh");
        result.Data.User.Email.Should().Be("user@example.com");
    }

    [Fact]
    public async Task LoginAsync_ReturnsFailure_WhenPasswordInvalid()
    {
        var user = SampleUser();
        _userRepository.Setup(r => r.GetByEmailAsync("user@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify(user.PasswordHash, "wrong")).Returns(false);

        var result = await _sut.LoginAsync(
            new LoginRequest { Email = "user@example.com", Password = "wrong" },
            CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Errors.Should().Contain("Invalid email or password.");
    }

    [Fact]
    public async Task RegisterAsync_ReturnsFailure_WhenEmailAlreadyExists()
    {
        _userRepository.Setup(r => r.ExistsByEmailAsync("user@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.RegisterAsync(
            new RegisterRequest
            {
                Email = "user@example.com",
                Password = "secret123",
                DisplayName = "User"
            },
            CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Errors.Should().Contain("An account with that email already exists.");
    }

    [Fact]
    public async Task RefreshAsync_RotatesToken_WhenRefreshTokenValid()
    {
        var user = SampleUser();
        var stored = new RefreshToken
        {
            RefreshTokenId = Guid.NewGuid(),
            UserId = user.UserId,
            User = user,
            TokenHash = "hash-refresh",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow
        };

        _refreshTokenRepository.Setup(r => r.GetByHashAsync("hash-refresh", It.IsAny<CancellationToken>()))
            .ReturnsAsync(stored);
        _refreshTokenRepository.Setup(r => r.UpdateAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _refreshTokenRepository.Setup(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken token, CancellationToken _) => token);

        var result = await _sut.RefreshAsync(
            new RefreshTokenRequest { RefreshToken = "raw-refresh" },
            CancellationToken.None);

        result.Success.Should().BeTrue();
        stored.RevokedAt.Should().NotBeNull();
        _refreshTokenRepository.Verify(r => r.UpdateAsync(stored, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ForgotPasswordAsync_SendsLink_WhenUserExists()
    {
        var user = SampleUser();
        _userRepository.Setup(r => r.GetByEmailAsync("user@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordResetTokenRepository.Setup(r => r.AddAsync(It.IsAny<PasswordResetToken>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PasswordResetToken token, CancellationToken _) => token);

        var result = await _sut.ForgotPasswordAsync(
            new ForgotPasswordRequest { Email = "user@example.com" },
            CancellationToken.None);

        result.Success.Should().BeTrue();
        _passwordResetNotifier.Verify(
            n => n.SendResetLinkAsync("user@example.com", "raw-reset", It.IsAny<CancellationToken>(), false),
            Times.Once);
    }

    [Fact]
    public async Task ForgotPasswordAsync_ReturnsSuccess_WhenUserMissing()
    {
        _userRepository.Setup(r => r.GetByEmailAsync("missing@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppUser?)null);

        var result = await _sut.ForgotPasswordAsync(
            new ForgotPasswordRequest { Email = "missing@example.com" },
            CancellationToken.None);

        result.Success.Should().BeTrue();
        _passwordResetNotifier.Verify(
            n => n.SendResetLinkAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), false),
            Times.Never);
    }

    [Fact]
    public async Task ResetPasswordAsync_UpdatesPassword_WhenTokenValid()
    {
        var user = SampleUser();
        var stored = new PasswordResetToken
        {
            PasswordResetTokenId = Guid.NewGuid(),
            UserId = user.UserId,
            User = user,
            TokenHash = "hash-reset",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow
        };

        _passwordResetTokenRepository.Setup(r => r.GetByHashAsync("hash-reset", It.IsAny<CancellationToken>()))
            .ReturnsAsync(stored);
        _passwordHasher.Setup(h => h.Hash("newsecret123")).Returns("new-hash");
        _userRepository.Setup(r => r.UpdateAsync(It.IsAny<AppUser>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _passwordResetTokenRepository.Setup(r => r.UpdateAsync(It.IsAny<PasswordResetToken>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _refreshTokenRepository.Setup(r => r.RevokeAllActiveForUserAsync(user.UserId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.ResetPasswordAsync(
            new ResetPasswordRequest { Token = "raw-reset", NewPassword = "newsecret123" },
            CancellationToken.None);

        result.Success.Should().BeTrue();
        user.PasswordHash.Should().Be("new-hash");
        stored.UsedAt.Should().NotBeNull();
    }

    private static AppUser SampleUser() => new()
    {
        UserId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
        Email = "user@example.com",
        PasswordHash = "hashed",
        DisplayName = "User",
        Role = TenantRole.Owner,
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };
}
