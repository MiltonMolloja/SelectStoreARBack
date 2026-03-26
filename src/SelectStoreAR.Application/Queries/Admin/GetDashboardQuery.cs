using MediatR;
using SelectStoreAR.Application.DTOs;
using SelectStoreAR.Domain.Entities;
using SelectStoreAR.Domain.Enums;
using SelectStoreAR.Domain.Interfaces;

namespace SelectStoreAR.Application.Queries.Admin;

public sealed record GetDashboardQuery : IRequest<DashboardDto>;

public sealed class GetDashboardHandler(
    IProductRepository productRepository,
    IOrderRepository orderRepository,
    IExchangeRateRepository exchangeRateRepository)
    : IRequestHandler<GetDashboardQuery, DashboardDto>
{
    private const int DeliveryAlertDays = 7;

    public async Task<DashboardDto> Handle(GetDashboardQuery request, CancellationToken cancellationToken)
    {
        Task<(IReadOnlyList<Product>, int)> allProductsTask = productRepository.GetPagedAsync(
            1, 1000, status: null, cancellationToken: cancellationToken);

        Task<(IReadOnlyList<Order>, int)> allOrdersTask = orderRepository.GetPagedAsync(
            1, 1000, cancellationToken: cancellationToken);

        Task<ExchangeRate?> rateTask = exchangeRateRepository.GetActiveAsync(cancellationToken);

        await Task.WhenAll(allProductsTask, allOrdersTask, rateTask).ConfigureAwait(false);

        (IReadOnlyList<Product> products, _) = await allProductsTask.ConfigureAwait(false);
        (IReadOnlyList<Order> orders, _) = await allOrdersTask.ConfigureAwait(false);
        ExchangeRate? rate = await rateTask.ConfigureAwait(false);

        // Products stats
        ProductStatsDto productStats = new(
            products.Count,
            products.Count(p => p.Status == ProductStatus.Active),
            products.Count(p => p.Status == ProductStatus.Draft),
            products.Count(p => p.Status == ProductStatus.Inactive));

        // Orders stats
        DateTime now = DateTime.UtcNow;
        DateTime startOfDay = now.Date;
        DateTime startOfWeek = now.AddDays(-(int)now.DayOfWeek);
        DateTime startOfMonth = new(now.Year, now.Month, 1);

        Dictionary<string, int> byStatus = Enum.GetValues<OrderStatus>()
            .ToDictionary(s => s.ToString(), s => orders.Count(o => o.Status == s));

        List<Order> delivered = orders
            .Where(o => o.Status == OrderStatus.Delivered && o.StatusHistory.Count >= 2)
            .ToList();

        double avgDeliveryDays = delivered.Count > 0
            ? delivered.Average(o =>
            {
                DateTime created = o.CreatedAt;
                DateTime? deliveredAt = o.StatusHistory
                    .LastOrDefault(h => h.Status == OrderStatus.Delivered)?.ChangedAt;
                return deliveredAt.HasValue ? (deliveredAt.Value - created).TotalDays : 0;
            })
            : 0;

        int delayed = orders
            .Count(o => o.Status is not (OrderStatus.Delivered or OrderStatus.Cancelled)
                && (now - o.UpdatedAt).TotalDays > DeliveryAlertDays);

        OrderStatsDto orderStats = new(
            orders.Count(o => o.CreatedAt >= startOfDay),
            orders.Count(o => o.CreatedAt >= startOfWeek),
            orders.Count(o => o.CreatedAt >= startOfMonth),
            byStatus,
            Math.Round(avgDeliveryDays, 1),
            delayed);

        // Top products by order count
        List<TopProductDto> topProducts = orders
            .Where(o => o.Status != OrderStatus.Cancelled)
            .SelectMany(o => o.Items)
            .GroupBy(i => new { i.ProductSlug, i.ProductName })
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => new TopProductDto(g.Key.ProductName, g.Key.ProductSlug, g.Count()))
            .ToList();

        ExchangeRateDto? rateDto = rate is not null
            ? new ExchangeRateDto(rate.Id, rate.Rate, rate.Type, rate.IsActive, rate.IsStale(), rate.UpdatedAt)
            : null;

        return new DashboardDto(productStats, orderStats, rateDto, topProducts);
    }
}
