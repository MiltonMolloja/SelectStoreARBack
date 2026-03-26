using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.EnableRetryOnFailure(3);
                npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
            });
        });

        // Redis
        string redisConnection = configuration.GetConnectionString("Redis")
            ?? "localhost:6379";

        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(redisConnection));

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnection;
            options.InstanceName = "selectstorear:";
        });

        // UnitOfWork
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Repositories
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IExchangeRateRepository, ExchangeRateRepository>();
        services.AddScoped<ISiteConfigRepository, SiteConfigRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        // Cache
        services.AddScoped<ICacheService, CacheService>();

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
        using IServiceScope scope = serviceProvider.CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.Database.MigrateAsync().ConfigureAwait(false);
        await DbSeeder.SeedAsync(dbContext).ConfigureAwait(false);
    }
}
