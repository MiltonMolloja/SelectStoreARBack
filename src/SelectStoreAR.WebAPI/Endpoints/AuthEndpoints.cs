using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using SelectStoreAR.Application.Commands.Auth;
using SelectStoreAR.Application.DTOs;
using SelectStoreAR.Application.Queries.Auth;

namespace SelectStoreAR.WebAPI.Endpoints;

public static class AuthEndpoints
{
    private const string CookieName = "token";

    public static void MapAuthEndpoints(this WebApplication app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/auth").WithTags("Auth");

        // OAuth challenge endpoints
        group.MapGet("/google", ChallengeGoogle).AllowAnonymous();
        group.MapGet("/facebook", ChallengeFacebook).AllowAnonymous();

        // Callbacks
        group.MapGet("/google/callback", GoogleCallback).AllowAnonymous();
        group.MapGet("/facebook/callback", FacebookCallback).AllowAnonymous();

        // Session
        group.MapGet("/me", GetCurrentUser).RequireAuthorization();
        group.MapPost("/logout", Logout);

        // Dev-only: genera JWT de admin sin OAuth (solo en Development)
        if (app.Environment.IsDevelopment())
        {
            group.MapPost("/dev-admin-token", DevAdminToken)
                .AllowAnonymous()
                .WithDescription("DEV ONLY — genera JWT de admin para testing");

            group.MapGet("/dev-claims", (HttpContext ctx) =>
                Results.Ok(ctx.User.Claims.Select(c => new { c.Type, c.Value }).ToList()))
                .AllowAnonymous();
        }

        // User profile
        RouteGroupBuilder userGroup = app.MapGroup("/api/user").WithTags("User");
        userGroup.MapGet("/orders", GetUserOrders).RequireAuthorization();
    }

    private static IResult ChallengeGoogle(HttpContext httpContext)
    {
        string redirectUrl = "/api/auth/google/callback";
        AuthenticationProperties properties = new() { RedirectUri = redirectUrl };
        return Results.Challenge(properties, ["Google"]);
    }

    private static IResult ChallengeFacebook(HttpContext httpContext)
    {
        string redirectUrl = "/api/auth/facebook/callback";
        AuthenticationProperties properties = new() { RedirectUri = redirectUrl };
        return Results.Challenge(properties, ["Facebook"]);
    }

    private static async Task<IResult> GoogleCallback(
        HttpContext httpContext,
        ISender sender,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        return await HandleOAuthCallback(httpContext, sender, configuration, "Google", cancellationToken);
    }

    private static async Task<IResult> FacebookCallback(
        HttpContext httpContext,
        ISender sender,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        return await HandleOAuthCallback(httpContext, sender, configuration, "Facebook", cancellationToken);
    }

    private static async Task<IResult> HandleOAuthCallback(
        HttpContext httpContext,
        ISender sender,
        IConfiguration configuration,
        string provider,
        CancellationToken cancellationToken)
    {
        AuthenticateResult result = await httpContext.AuthenticateAsync(CookieScheme(provider));
        if (!result.Succeeded || result.Principal is null)
        {
            string frontendUrl = configuration["Frontend:Url"] ?? "http://localhost:4200";
            return Results.Redirect($"{frontendUrl}/auth/error");
        }

        ClaimsPrincipal principal = result.Principal;

        string? providerKey = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        string? email = principal.FindFirstValue(ClaimTypes.Email);
        string? name = principal.FindFirstValue(ClaimTypes.Name);
        string? pictureUrl = principal.FindFirstValue("picture") ?? principal.FindFirstValue("urn:google:picture");

        if (providerKey is null || email is null || name is null)
        {
            string frontendUrl = configuration["Frontend:Url"] ?? "http://localhost:4200";
            return Results.Redirect($"{frontendUrl}/auth/error");
        }

        OAuthLoginResult loginResult = await sender.Send(
            new OAuthLoginCommand(provider, providerKey, email, name, pictureUrl),
            cancellationToken);

        // Set httpOnly JWT cookie
        CookieOptions cookieOptions = new()
        {
            HttpOnly = true,
            Secure = !httpContext.Request.IsHttps ? false : true,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(7),
            Path = "/",
        };

        httpContext.Response.Cookies.Append(CookieName, loginResult.Token, cookieOptions);

        string successUrl = configuration["Frontend:Url"] ?? "http://localhost:4200";
        return Results.Redirect($"{successUrl}/auth/success");
    }

    private static async Task<IResult> GetCurrentUser(
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        string? userIdStr = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? httpContext.User.FindFirstValue("sub");

        if (!Guid.TryParse(userIdStr, out Guid userId))
        {
            return Results.Unauthorized();
        }

        UserDto? user = await sender.Send(new GetCurrentUserQuery(userId), cancellationToken);
        return user is null ? Results.NotFound() : Results.Ok(user);
    }

    private static IResult Logout(HttpContext httpContext)
    {
        httpContext.Response.Cookies.Delete(CookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = !httpContext.Request.IsHttps ? false : true,
            SameSite = SameSiteMode.Lax,
            Path = "/",
        });

        return Results.Ok(new { message = "Logged out successfully" });
    }

    private static async Task<IResult> GetUserOrders(
        HttpContext httpContext,
        ISender sender,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        string? userIdStr = httpContext.User.FindFirstValue("sub")
            ?? httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdStr, out Guid userId))
        {
            return Results.Unauthorized();
        }

        // TODO: implement GetUserOrdersQuery
        return Results.Ok(new PagedResult<OrderDto>([], 0, page, pageSize));
    }

    private static string CookieScheme(string provider) =>
        provider == "Google" ? "Google" : "Facebook";

    /// <summary>
    /// DEV ONLY — genera un JWT de admin sin OAuth para testing local.
    /// Solo disponible en Development.
    /// </summary>
    private static IResult DevAdminToken(
        SelectStoreAR.Application.Interfaces.IJwtService jwtService,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        string secret = configuration["Auth:JwtSecret"] ?? string.Empty;
        if (string.IsNullOrEmpty(secret))
        {
            return Results.Problem("Auth:JwtSecret not configured");
        }

        // Crear un usuario admin ficticio para dev
        SelectStoreAR.Domain.Entities.User adminUser = SelectStoreAR.Domain.Entities.User.Create(
            "dev-admin@selectstorear.com",
            "Dev Admin",
            "admin");

        string token = jwtService.GenerateToken(adminUser);
        return Results.Ok(new { token, expiresIn = "7 days", role = "admin" });
    }
}
