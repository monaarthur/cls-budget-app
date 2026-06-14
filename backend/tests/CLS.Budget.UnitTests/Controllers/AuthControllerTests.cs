using System.Security.Claims;
using CLS.Budget.Api.Controllers;
using CLS.Budget.Application.Abstractions.Services;
using CLS.Budget.Application.Auth.Dtos;
using CLS.Budget.Application.Common;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace CLS.Budget.UnitTests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _service = new();
    private readonly AuthController _sut;

    public AuthControllerTests()
    {
        _sut = new AuthController(_service.Object);
        ControllerTestHelper.AttachControllerContext(_sut, "Auth");
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenCredentialsInvalid()
    {
        _service.Setup(s => s.LoginAsync(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<AuthResponse>.Fail("Invalid email or password."));

        var result = await _sut.Login(
            new LoginRequest { Email = "user@example.com", Password = "wrong" },
            CancellationToken.None);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Me_ReturnsOk_WhenUserExists()
    {
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        _sut.ControllerContext.HttpContext!.User = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim("sub", userId.ToString())], "Test"));

        _service.Setup(s => s.GetCurrentUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<CurrentUserResponse>.Ok(new CurrentUserResponse
            {
                UserId = userId,
                TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                Email = "user@example.com",
                DisplayName = "User",
                Role = "Owner"
            }));

        var result = await _sut.Me(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var envelope = ok.Value.Should().BeOfType<ApiResponse<CurrentUserResponse>>().Subject;
        envelope.Success.Should().BeTrue();
        envelope.Data!.Email.Should().Be("user@example.com");
    }
}
