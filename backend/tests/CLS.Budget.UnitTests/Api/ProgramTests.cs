using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CLS.Budget.Application.Accounts.Dtos;
using CLS.Budget.Application.Common;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CLS.Budget.UnitTests.Api;

public class ProgramTests : IClassFixture<BudgetWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _client;

    public ProgramTests(BudgetWebApplicationFactory factory) =>
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    [Fact]
    public async Task Host_Starts_AndMapsAccountRoutes()
    {
        var response = await _client.GetAsync("/api/v1/accounts");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await ReadEnvelopeAsync<IReadOnlyList<AccountResponse>>(response);
        envelope.Success.Should().BeTrue();
        envelope.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task Development_Environment_ExposesSwagger()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("/api/v1/Accounts");
    }

    [Fact]
    public async Task InvalidModelState_ReturnsApiResponseEnvelope()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/accounts", new { });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var envelope = await ReadEnvelopeAsync<object>(response);
        envelope.Success.Should().BeFalse();
        envelope.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task FluentValidation_ReturnsBadRequest_ForInvalidCreateAccount()
    {
        var request = new CreateAccountRequest
        {
            Name = "",
            Number = "",
            Balance = 0,
            Limit = 0,
            AccountOpenDate = default,
            Phone = "",
            Email = "",
            Url = "",
            AccountCategoryId = 0
        };

        var response = await _client.PostAsJsonAsync("/api/v1/accounts", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var envelope = await ReadEnvelopeAsync<AccountResponse>(response);
        envelope.Success.Should().BeFalse();
        envelope.Errors.Should().NotBeEmpty();
    }

    private static async Task<ApiResponse<T>> ReadEnvelopeAsync<T>(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        var envelope = JsonSerializer.Deserialize<ApiResponse<T>>(json, JsonOptions);
        envelope.Should().NotBeNull();
        return envelope!;
    }
}
