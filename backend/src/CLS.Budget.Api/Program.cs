using System.Text;
using CLS.Budget.Api.Auth;
using CLS.Budget.Application;
using CLS.Budget.Application.Abstractions;
using CLS.Budget.Application.Common;
using CLS.Budget.Infrastructure;
using CLS.Budget.Infrastructure.Auth;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseWindowsService();

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState.Values
                .SelectMany(entry => entry.Errors)
                .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                    ? "Validation failed."
                    : error.ErrorMessage)
                .ToList();

            return new BadRequestObjectResult(ApiResponse<object>.Fail(errors));
        };
    });

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Resolve the tenant from the request's claims (falls back to the seeded
// default tenant until auth enforcement is enabled).
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantContext, HttpTenantContext>();

var authOptions = builder.Configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>()
    ?? new AuthOptions();
builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection(AuthOptions.SectionName));

// Guardrail: authentication may only be disabled in Development.
if (!authOptions.Enabled && !builder.Environment.IsDevelopment())
{
    throw new InvalidOperationException(
        "Auth:Enabled cannot be false outside the Development environment.");
}

if (authOptions.Enabled)
{
    var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
        ?? new JwtOptions();

    if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey))
    {
        throw new InvalidOperationException(
            "Jwt:SigningKey must be configured when authentication is enabled.");
    }

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            // Keep the original JWT claim names ("sub", "tenant_id") instead of
            // remapping them to the long ClaimTypes URIs.
            options.MapInboundClaims = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtOptions.Audience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
                ClockSkew = TimeSpan.FromSeconds(30),
                NameClaimType = "name",
                RoleClaimType = System.Security.Claims.ClaimTypes.Role
            };
        });
}
else
{
    // Dev-only: every request is authenticated as the configured dev user/tenant.
    builder.Services.AddAuthentication(DevAuthenticationHandler.SchemeName)
        .AddScheme<AuthenticationSchemeOptions, DevAuthenticationHandler>(
            DevAuthenticationHandler.SchemeName, _ => { });
}

builder.Services.AddAuthorization(options =>
{
    // Any endpoint without explicit metadata requires an authenticated tenant user.
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .RequireRole(AuthorizationPolicies.MemberRole, AuthorizationPolicies.OwnerRole)
        .Build();

    options.AddPolicy(AuthorizationPolicies.TenantMember, policy =>
        policy.RequireRole(AuthorizationPolicies.MemberRole, AuthorizationPolicies.OwnerRole));

    options.AddPolicy(AuthorizationPolicies.TenantOwner, policy =>
        policy.RequireRole(AuthorizationPolicies.OwnerRole));
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var scheme = new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter the JWT access token (without the 'Bearer' prefix).",
        Reference = new Microsoft.OpenApi.Models.OpenApiReference
        {
            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
            Id = "Bearer"
        }
    };

    options.AddSecurityDefinition("Bearer", scheme);
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        [scheme] = []
    });
});

var productionCorsOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()
    ?.Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Select(origin => origin.Trim())
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendDev", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });

    if (productionCorsOrigins.Length > 0)
    {
        options.AddPolicy("FrontendProduction", policy =>
        {
            policy.WithOrigins(productionCorsOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
    }
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors("FrontendDev");
}
else
{
    app.UseHttpsRedirection();

    if (productionCorsOrigins.Length > 0)
    {
        app.UseCors("FrontendProduction");
    }
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;
