using SelectStoreAR.Domain.Common;

namespace SelectStoreAR.Domain.Entities;

public sealed class ProductImage : BaseEntity
{
    public Guid Id { get; private set; }

    public Guid ProductId { get; private set; }

    public string Url { get; private set; } = string.Empty;

    public string ThumbnailUrl { get; private set; } = string.Empty;

    public string MediumUrl { get; private set; } = string.Empty;

    public int SortOrder { get; private set; }

    public string? AltText { get; private set; }

    public DateTime CreatedAt { get; private set; }

    private ProductImage()
    {
    }

    public static ProductImage Create(Guid productId, string url, int order)
    {
        return new ProductImage
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            Url = url,
            ThumbnailUrl = url.Replace("/original", "/thumb", StringComparison.Ordinal),
            MediumUrl = url.Replace("/original", "/medium", StringComparison.Ordinal),
            SortOrder = order,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public void SetOrder(int order) => SortOrder = order;

    public void SetAltText(string altText) => AltText = altText;
}
