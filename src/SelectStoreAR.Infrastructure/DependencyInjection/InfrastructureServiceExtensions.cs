using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using SelectStoreAR.Application.Interfaces;
using SelectStoreAR.Domain.Interfaces;
using SelectStoreAR.Infrastructure.Persistence;
using SelectStoreAR.Infrastructure.Persistence.Repositories;
using SelectStoreAR.Infrastructure.Services;
using StackExchange.Redis;

namespace SelectStoreAR.Infrastructure.DependencyInjection;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        string connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' not found.");

        services.AddDbContext<AppDbContext>(options =>
        {
            NpgsqlDataSourceBuilder dataSourceBuilder = new(connectionString);
            dataSourceBuilder.EnableDynamicJson();
            NpgsqlDataSource dataSource = dataSourceBuilder.Build();

            options.UseNpgsql(dataSource, npgsql =>
            {
                npgsql.EnableRetryOnFailure(3);
                npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
            });

            // Suprimir warning de cambios pendientes en el modelo cuando el esquema
            // real de la DB ya es correcto (ej: quitar HasDefaultValue sin cambio de columna)
            options.ConfigureWarnings(w =>
                w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });

        // Redis — desactivado en Development, activo en Production
        bool useRedis = !string.Equals(
            configuration["ASPNETCORE_ENVIRONMENT"] ?? configuration["Environment"],
            "Development",
            StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrEmpty(configuration.GetConnectionString("Redis"));

        if (useRedis)
        {
            string redisConnection = configuration.GetConnectionString("Redis")!;

            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(redisConnection));

            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnection;
                options.InstanceName = "selectstorear:";
            });
        }
        else
        {
            // En Development: cache en memoria, sin Redis
            services.AddDistributedMemoryCache();
            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect("localhost:6379,abortConnect=false"));
        }

        // UnitOfWork
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Repositories
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IExchangeRateRepository, ExchangeRateRepository>();
        services.AddScoped<ISiteConfigRepository, SiteConfigRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IProductImageRepository, ProductImageRepository>();
        services.AddScoped<IPendingChangeRepository, PendingChangeRepository>();
        services.AddScoped<IPriceHistoryRepository, PriceHistoryRepository>();

        // Notifications: WhatsApp (Twilio) + Email (SMTP) via composite
        services.AddScoped<TwilioWhatsAppService>();
        services.AddScoped<EmailNotificationService>();
        services.AddScoped<INotificationService, CompositeNotificationService>();

        // Cache — NoOp en Development, Redis en Production
        if (useRedis)
        {
            services.AddScoped<ICacheService, CacheService>();
        }
        else
        {
            services.AddScoped<ICacheService, NoOpCacheService>();
        }

        // Auth
        services.AddScoped<IJwtService, JwtService>();

        // Image
        services.AddScoped<IImageService, ImageService>();
        services.AddScoped<ITelegramImageService, TelegramImageService>();

        services.AddHttpClient(); // For Telegram image download

        return services;
    }

    public static async Task MigrateAndSeedAsync(IServiceProvider serviceProvider)
    {
        try
        {
            using IServiceScope scope = serviceProvider.CreateScope();
            AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await dbContext.Database.MigrateAsync().ConfigureAwait(false);
            await DbSeeder.SeedAsync(dbContext).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Don't crash the app if DB is unavailable at startup.
            // The health endpoint will report the DB as unhealthy.
            ILogger<AppDbContext> logger = serviceProvider
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger<AppDbContext>();

            logger.LogError(ex, "Failed to migrate/seed database at startup. The app will continue but DB-dependent features will fail until the database is available.");
        }
    }
}
