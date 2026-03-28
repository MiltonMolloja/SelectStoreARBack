using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

using NetEscapades.AspNetCore.SecurityHeaders;
using Scalar.AspNetCore;
using Serilog;
using SelectStoreAR.Application.DependencyInjection;
using SelectStoreAR.Infrastructure.DependencyInjection;
using SelectStoreAR.WebAPI.Endpoints;
using SelectStoreAR.WebAPI.Middleware;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    // ── Logging ─────────────────────────────────────────────────────────────
    builder.Host.UseSerilog((ctx, services, cfg) =>
    {
        cfg.ReadFrom.Configuration(ctx.Configuration)
           .ReadFrom.Services(services)
           .Enrich.FromLogContext()
           .Enrich.WithProperty("Application", "SelectStoreAR.API")
           .WriteTo.Console();
    });

    // ── Application + Infrastructure ────────────────────────────────────────
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // ── Auth ─────────────────────────────────────────────────────────────────
    string jwtSecret = builder.Configuration["Auth:JwtSecret"] ?? "change-me-in-production-secret-key-256bits";
    string jwtIssuer = builder.Configuration["Auth:JwtIssuer"] ?? "selectstorear";
    string jwtAudience = builder.Configuration["Auth:JwtAudience"] ?? "selectstorear";

    builder.Services
        .AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                RoleClaimType = "role",
            };

            // Leer el JWT desde la cookie httpOnly
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = ctx =>
                {
                    if (ctx.Request.Cookies.TryGetValue("token", out string? token))
                    {
                        ctx.Token = token;
                    }

                    return Task.CompletedTask;
                },
            };
        })
        .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddGoogle(options =>
        {
            options.ClientId = builder.Configuration["Auth:Google:ClientId"] ?? "google-client-id";
            options.ClientSecret = builder.Configuration["Auth:Google:ClientSecret"] ?? "google-client-secret";
            options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.SaveTokens = true;
            options.ClaimActions.MapJsonKey("picture", "picture", "string");
        })
        .AddFacebook(options =>
        {
            options.AppId = builder.Configuration["Auth:Facebook:AppId"] ?? "facebook-app-id";
            options.AppSecret = builder.Configuration["Auth:Facebook:AppSecret"] ?? "facebook-app-secret";
            options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.SaveTokens = true;
            options.Fields.Add("picture");
        });

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("admin", policy =>
            policy.RequireClaim("role", "admin"));
    });

    // ── Security headers ────────────────────────────────────────────────────
    builder.Services.AddSecurityHeaderPolicies()
        .SetDefaultPolicy(p => p
            .AddFrameOptionsDeny()
            .AddContentTypeOptionsNoSniff()
            .AddStrictTransportSecurityMaxAgeIncludeSubDomains(maxAgeInSeconds: 60 * 60 * 24 * 365)
            .AddReferrerPolicyStrictOriginWhenCrossOrigin()
            .RemoveServerHeader());

    // ── CORS ─────────────────────────────────────────────────────────────────
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("Frontend", policy =>
        {
            string[] origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
                ?? ["http://localhost:4200"];

            policy.WithOrigins(origins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });

    // ── Rate Limiting ────────────────────────────────────────────────────────
    builder.Services.AddRateLimiter(options =>
    {
        options.GlobalLimiter = System.Threading.RateLimiting.PartitionedRateLimiter
            .Create<HttpContext, string>(ctx =>
                System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                    ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }));

        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    });

    // ── Output Cache ─────────────────────────────────────────────────────────
    builder.Services.AddOutputCache(options =>
    {
        options.AddBasePolicy(b => b.Cache().Expire(TimeSpan.FromMinutes(5)));
        options.AddPolicy("landing", b => b.Expire(TimeSpan.FromMinutes(10)).Tag("products").Tag("categories"));
        options.AddPolicy("categories", b => b.Expire(TimeSpan.FromMinutes(15)).Tag("categories"));
        options.AddPolicy("products", b => b.Expire(TimeSpan.FromMinutes(5)).Tag("products"));
    });

    // ── Response Compression ─────────────────────────────────────────────────
    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
    });

    // ── Exception handling ───────────────────────────────────────────────────
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    // ── API Docs ─────────────────────────────────────────────────────────────
    builder.Services.AddOpenApi();

    // ════════════════════════════════════════════════════════════════════════
    WebApplication app = builder.Build();

    await InfrastructureServiceExtensions.MigrateAndSeedAsync(app.Services);

    // ── Middleware pipeline ───────────────────────────────────────────────────
    app.UseSecurityHeaders();
    app.UseResponseCompression();
    app.UseSerilogRequestLogging();
    app.UseExceptionHandler();

    // Servir imágenes de productos desde /uploads/
    string uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
    Directory.CreateDirectory(uploadsPath);
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
        RequestPath = "/uploads",
        OnPrepareResponse = ctx =>
        {
            // Cache de imágenes por 7 días
            ctx.Context.Response.Headers.CacheControl = "public, max-age=604800";
        },
    });

    if (app.Environment.IsDevelopment())
    {
        // OpenAPI JSON spec: /openapi/v1.json
        app.MapOpenApi();

        // Swagger UI (CDN): /swagger
        app.MapGet("/swagger", () => Results.Content(
            $$"""
            <!DOCTYPE html>
            <html>
            <head>
              <title>SelectStoreAR API — Swagger UI</title>
              <meta charset="utf-8"/>
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <link rel="stylesheet" type="text/css" href="https://unpkg.com/swagger-ui-dist@5/swagger-ui.css">
            </head>
            <body>
            <div id="swagger-ui"></div>
            <script src="https://unpkg.com/swagger-ui-dist@5/swagger-ui-bundle.js"></script>
            <script src="https://unpkg.com/swagger-ui-dist@5/swagger-ui-standalone-preset.js"></script>
            <script>
              window.onload = function() {
                SwaggerUIBundle({
                  url: "/openapi/v1.json",
                  dom_id: '#swagger-ui',
                  presets: [SwaggerUIBundle.presets.apis, SwaggerUIStandalonePreset],
                  layout: "StandaloneLayout",
                  persistAuthorization: true,
                  displayRequestDuration: true,
                  filter: true,
                  tryItOutEnabled: true
                });
              };
            </script>
            </body>
            </html>
            """,
            "text/html"))
            .ExcludeFromDescription();

        // Scalar UI: /scalar/v1
        app.MapScalarApiReference(options =>
        {
            options.Title = "SelectStoreAR API";
            options.Theme = ScalarTheme.DeepSpace;
            options.DefaultHttpClient = new(ScalarTarget.CSharp, ScalarClient.HttpClient);
            options.Authentication = new ScalarAuthenticationOptions
            {
                PreferredSecuritySchemes = ["Bearer"],
            };
        });
    }

    app.UseRateLimiter();
    app.UseCors("Frontend");
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseOutputCache();

    // ── Static files (product images) ─────────────────────────────────────────
    app.UseStaticFiles();

    // ── Endpoints ─────────────────────────────────────────────────────────────
    app.MapAuthEndpoints();
    app.MapUserEndpoints();
    app.MapLandingEndpoints();
    app.MapSearchEndpoints();
    app.MapProductEndpoints();
    app.MapCategoryEndpoints();
    app.MapOrderEndpoints();
    app.MapExchangeRateEndpoints();
    app.MapAdminEndpoints();
    app.MapTelegramEndpoints();
    app.MapPendingChangesEndpoints();

    app.MapGet("/health", async (IServiceProvider services) =>
    {
        string dbStatus = "unknown";
        string redisStatus = "unknown";

        try
        {
            using IServiceScope scope = services.CreateScope();
            SelectStoreAR.Infrastructure.Persistence.AppDbContext dbContext = scope.ServiceProvider
                .GetRequiredService<SelectStoreAR.Infrastructure.Persistence.AppDbContext>();
            await dbContext.Database.CanConnectAsync();
            dbStatus = "healthy";
        }
        catch
        {
            dbStatus = "unhealthy";
        }

        try
        {
            StackExchange.Redis.IConnectionMultiplexer redis = services
                .GetRequiredService<StackExchange.Redis.IConnectionMultiplexer>();
            StackExchange.Redis.IDatabase db = redis.GetDatabase();
            await db.PingAsync();
            redisStatus = "healthy";
        }
        catch
        {
            redisStatus = "unhealthy";
        }

        bool allHealthy = dbStatus == "healthy" && redisStatus == "healthy";
        int statusCode = allHealthy ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable;

        object response = new
        {
            status = allHealthy ? "healthy" : "unhealthy",
            checks = new { database = dbStatus, redis = redisStatus },
            timestamp = DateTime.UtcNow,
        };

        return Results.Json(response, statusCode: statusCode);
    }).WithTags("Health").AllowAnonymous();

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
