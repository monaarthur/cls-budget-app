using CLS.Budget.Application.Abstractions;
using CLS.Budget.Application.Abstractions.Repositories;
using CLS.Budget.Application.Abstractions.Services;
using CLS.Budget.Application.Auth.Dtos;
using CLS.Budget.Application.Common;
using CLS.Budget.Domain.Entities;

namespace CLS.Budget.Application.Auth;

public sealed class AuthService(
    IAppUserRepository userRepository,
    ITenantRepository tenantRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IPasswordHasher passwordHasher,
    ITokenService tokenService) : IAuthService
{
    private const string InvalidCredentials = "Invalid email or password.";

    public async Task<ApiResponse<AuthResponse>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = Normalize(request.Email);

        if (await userRepository.ExistsByEmailAsync(email, cancellationToken))
        {
            return ApiResponse<AuthResponse>.Fail("An account with that email already exists.");
        }

        var tenantName = string.IsNullOrWhiteSpace(request.TenantName)
            ? $"{request.DisplayName.Trim()}'s Household"
            : request.TenantName.Trim();

        var now = DateTime.UtcNow;
        var tenant = await tenantRepository.AddAsync(
            new Tenant
            {
                TenantId = Guid.NewGuid(),
                Name = tenantName,
                IsActive = true,
                CreatedAt = now
            },
            cancellationToken);

        var user = await userRepository.AddAsync(
            new AppUser
            {
                UserId = Guid.NewGuid(),
                TenantId = tenant.TenantId,
                Email = email,
                PasswordHash = passwordHasher.Hash(request.Password),
                DisplayName = request.DisplayName.Trim(),
                Role = TenantRole.Owner,
                IsActive = true,
                CreatedAt = now
            },
            cancellationToken);

        var auth = await IssueTokensAsync(user, cancellationToken);
        return ApiResponse<AuthResponse>.Ok(auth);
    }

    public async Task<ApiResponse<AuthResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = Normalize(request.Email);
        var user = await userRepository.GetByEmailAsync(email, cancellationToken);

        if (user is null || !user.IsActive)
        {
            return ApiResponse<AuthResponse>.Fail(InvalidCredentials);
        }

        if (!passwordHasher.Verify(user.PasswordHash, request.Password))
        {
            return ApiResponse<AuthResponse>.Fail(InvalidCredentials);
        }

        var auth = await IssueTokensAsync(user, cancellationToken);
        return ApiResponse<AuthResponse>.Ok(auth);
    }

    public async Task<ApiResponse<AuthResponse>> RefreshAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        var hash = tokenService.HashRefreshToken(request.RefreshToken);
        var stored = await refreshTokenRepository.GetByHashAsync(hash, cancellationToken);

        if (stored is null
            || stored.RevokedAt is not null
            || stored.ExpiresAt <= DateTime.UtcNow
            || stored.User is null
            || !stored.User.IsActive)
        {
            return ApiResponse<AuthResponse>.Fail("The refresh token is invalid or has expired.");
        }

        // Rotate: revoke the presented token before issuing a replacement.
        stored.RevokedAt = DateTime.UtcNow;
        await refreshTokenRepository.UpdateAsync(stored, cancellationToken);

        var auth = await IssueTokensAsync(stored.User, cancellationToken);
        return ApiResponse<AuthResponse>.Ok(auth);
    }

    public async Task<ApiResponse<CurrentUserResponse>> GetCurrentUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return ApiResponse<CurrentUserResponse>.Fail("User was not found.");
        }

        return ApiResponse<CurrentUserResponse>.Ok(AuthMapper.ToCurrentUser(user));
    }

    private async Task<AuthResponse> IssueTokensAsync(AppUser user, CancellationToken cancellationToken)
    {
        var accessToken = tokenService.CreateAccessToken(user);
        var refresh = tokenService.CreateRefreshToken();

        await refreshTokenRepository.AddAsync(
            new RefreshToken
            {
                RefreshTokenId = Guid.NewGuid(),
                UserId = user.UserId,
                TokenHash = refresh.TokenHash,
                ExpiresAt = refresh.ExpiresAt,
                CreatedAt = DateTime.UtcNow
            },
            cancellationToken);

        return new AuthResponse
        {
            AccessToken = accessToken.Token,
            AccessTokenExpiresAt = accessToken.ExpiresAt,
            RefreshToken = refresh.RawToken,
            RefreshTokenExpiresAt = refresh.ExpiresAt,
            User = AuthMapper.ToCurrentUser(user)
        };
    }

    private static string Normalize(string email) => email.Trim().ToLowerInvariant();
}
