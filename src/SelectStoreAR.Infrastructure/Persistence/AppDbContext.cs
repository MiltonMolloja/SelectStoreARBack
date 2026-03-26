using MediatR;
using Microsoft.EntityFrameworkCore;
using SelectStoreAR.Domain.Common;
using SelectStoreAR.Domain.Entities;

namespace SelectStoreAR.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options, IMediator mediator) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    public DbSet<ProductImage> ProductImages => Set<ProductImage>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    public DbSet<OrderStatusChange> OrderStatusChanges => Set<OrderStatusChange>();

    public DbSet<ExchangeRate> ExchangeRates => Set<ExchangeRate>();

    public DbSet<SiteConfig> SiteConfigs => Set<SiteConfig>();

    public DbSet<User> Users => Set<User>();

    public DbSet<ExternalLogin> ExternalLogins => Set<ExternalLogin>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        int result = await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await DispatchDomainEventsAsync(cancellationToken).ConfigureAwait(false);
        return result;
    }

    private async Task DispatchDomainEventsAsync(CancellationToken cancellationToken)
    {
        IEnumerable<BaseEntity> entities = ChangeTracker
            .Entries<BaseEntity>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        List<IDomainEvent> domainEvents = entities
            .SelectMany(e => e.DomainEvents)
            .ToList();

        foreach (BaseEntity entity in entities)
        {
            entity.ClearDomainEvents();
        }

        foreach (IDomainEvent domainEvent in domainEvents)
        {
            await mediator.Publish(domainEvent, cancellationToken).ConfigureAwait(false);
        }
    }
}
