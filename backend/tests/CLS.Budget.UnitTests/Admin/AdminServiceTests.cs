using CLS.Budget.Application.Abstractions;
using CLS.Budget.Application.Abstractions.Repositories;
using CLS.Budget.Application.Abstractions.Services;
using CLS.Budget.Application.Admin;
using CLS.Budget.Application.Admin.Dtos;
using CLS.Budget.Domain.Entities;
using FluentAssertions;
using Moq;

namespace CLS.Budget.UnitTests.Admin;

public class AdminServiceTests
{
    private readonly Mock<ITenantRepository> _tenantRepository = new();
    private readonly Mock<IAppUserRepository> _userRepository = new();
    private readonly Mock<IPasswordResetTokenRepository> _passwordResetTokenRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<IPasswordResetNotifier> _passwordResetNotifier = new();
    private readonly Mock<IPasswordResetSettings> _passwordResetSettings = new();
    private readonly AdminService _sut;

    public AdminServiceTests()
    {
        _passwordResetSettings.Setup(s => s.TokenLifetime).Returns(TimeSpan.FromHours(1));
        _passwordResetSettings.Setup(s => s.FrontendBaseUrl).Returns("http://localhost:3000");
        _passwordHasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hashed-password");
        _tokenService.Setup(t => t.CreateOneTimeToken(It.IsAny<TimeSpan>()))
            .Returns(new RefreshTokenResult("raw-reset", "hash-reset", DateTime.UtcNow.AddHours(1)));

        _sut = new AdminService(
            _tenantRepository.Object,
            _userRepository.Object,
            _passwordResetTokenRepository.Object,
            _passwordHasher.Object,
            _tokenService.Object,
            _passwordResetNotifier.Object,
            _passwordResetSettings.Object);
    }

    [Fact]
    public async Task InviteUserToTenantAsync_CreatesUserAndSendsInvite()
    {
        var tenantId = Guid.NewGuid();
        _tenantRepository.Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Tenant { TenantId = tenantId, Name = "MonaArthur", IsActive = true });
        _userRepository.Setup(r => r.GetByEmailAsync("owner@gmail.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppUser?)null);
        _userRepository.Setup(r => r.AddAsync(It.IsAny<AppUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppUser user, CancellationToken _) => user);
        _passwordResetTokenRepository.Setup(r => r.AddAsync(It.IsAny<PasswordResetToken>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PasswordResetToken token, CancellationToken _) => token);

        var result = await _sut.InviteUserToTenantAsync(
            new InviteTenantUserRequest
            {
                TenantId = tenantId,
                Email = "owner@gmail.com",
                DisplayName = "Mona",
                Role = "Owner"
            },
            CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Email.Should().Be("owner@gmail.com");
        result.Data.InviteSent.Should().BeTrue();
        result.Data.SetupLink.Should().Contain("/reset-password?token=");

        _passwordResetNotifier.Verify(
            n => n.SendResetLinkAsync(
                "owner@gmail.com",
                "raw-reset",
                It.IsAny<CancellationToken>(),
                true),
            Times.Once);
    }

    [Fact]
    public async Task InviteUserToTenantAsync_ResendsInviteForExistingEmailOnSameTenant()
    {
        var tenantId = Guid.NewGuid();
        var existingUser = new AppUser
        {
            UserId = Guid.NewGuid(),
            TenantId = tenantId,
            Email = "owner@gmail.com",
            DisplayName = "Mona",
            Role = TenantRole.Owner,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            PasswordHash = "hashed-password"
        };

        _tenantRepository.Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Tenant { TenantId = tenantId, Name = "Household", IsActive = true });
        _userRepository.Setup(r => r.GetByEmailAsync("owner@gmail.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);
        _passwordResetTokenRepository.Setup(r => r.AddAsync(It.IsAny<PasswordResetToken>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PasswordResetToken token, CancellationToken _) => token);

        var result = await _sut.InviteUserToTenantAsync(
            new InviteTenantUserRequest
            {
                TenantId = tenantId,
                Email = "owner@gmail.com",
                DisplayName = "Mona"
            },
            CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Email.Should().Be("owner@gmail.com");
        result.Data.InviteSent.Should().BeTrue();
        result.Data.SetupLink.Should().Contain("/reset-password?token=");

        _userRepository.Verify(
            r => r.AddAsync(It.IsAny<AppUser>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _passwordResetNotifier.Verify(
            n => n.SendResetLinkAsync(
                "owner@gmail.com",
                "raw-reset",
                It.IsAny<CancellationToken>(),
                true),
            Times.Once);
    }

    [Fact]
    public async Task InviteUserToTenantAsync_RejectsExistingEmailOnDifferentTenant()
    {
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        _tenantRepository.Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Tenant { TenantId = tenantId, Name = "Household", IsActive = true });
        _userRepository.Setup(r => r.GetByEmailAsync("owner@gmail.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppUser
            {
                UserId = Guid.NewGuid(),
                TenantId = otherTenantId,
                Email = "owner@gmail.com",
                DisplayName = "Mona",
                Role = TenantRole.Owner,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                PasswordHash = "hashed-password"
            });

        var result = await _sut.InviteUserToTenantAsync(
            new InviteTenantUserRequest
            {
                TenantId = tenantId,
                Email = "owner@gmail.com",
                DisplayName = "Mona"
            },
            CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Errors.Should().Contain("An account with that email already exists on another tenant.");
    }
}
