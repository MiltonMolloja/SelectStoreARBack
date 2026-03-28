using FluentAssertions;
using SelectStoreAR.Domain.Entities;
using SelectStoreAR.Domain.Enums;

namespace SelectStoreAR.Domain.Tests.Entities;

public sealed class PriceHistoryTests
{
    [Fact]
    public void Create_SetsAllFields()
    {
        Guid productId = Guid.NewGuid();
        Guid orderId = Guid.NewGuid();

        PriceHistory entry = PriceHistory.Create(
            productId,
            priceUsd: 1280m,
            source: PriceHistorySource.WhatsAppQuote,
            changedBy: "admin",
            orderId: orderId);

        entry.Id.Should().NotBeEmpty();
        entry.ProductId.Should().Be(productId);
        entry.PriceUsd.Amount.Should().Be(1280m);
        entry.Source.Should().Be(PriceHistorySource.WhatsAppQuote);
        entry.OrderId.Should().Be(orderId);
        entry.ChangedBy.Should().Be("admin");
        entry.ChangedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_WithoutOptionalFields_SetsNulls()
    {
        PriceHistory entry = PriceHistory.Create(
            Guid.NewGuid(),
            priceUsd: 500m,
            source: PriceHistorySource.Approved);

        entry.OrderId.Should().BeNull();
        entry.ChangedBy.Should().BeNull();
    }

    [Theory]
    [InlineData(PriceHistorySource.TelegramSync)]
    [InlineData(PriceHistorySource.WhatsAppQuote)]
    [InlineData(PriceHistorySource.Approved)]
    [InlineData(PriceHistorySource.Manual)]
    public void Create_AcceptsAllSources(PriceHistorySource source)
    {
        PriceHistory entry = PriceHistory.Create(Guid.NewGuid(), 100m, source);

        entry.Source.Should().Be(source);
    }
}
