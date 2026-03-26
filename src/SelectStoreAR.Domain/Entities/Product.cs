using SelectStoreAR.Domain.Common;
using SelectStoreAR.Domain.Enums;
using SelectStoreAR.Domain.Events;
using SelectStoreAR.Domain.ValueObjects;

namespace SelectStoreAR.Domain.Entities;

public sealed class Product : BaseEntity
{
    private readonly List<ProductImage> _images = [];

    private Product()
    {
    }

    public Guid Id { get; private set; }

    public Slug Slug { get; private set; } = null!;

    public string Name { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public string Brand { get; private set; } = string.Empty;

    public Money BasePriceUsd { get; private set; } = null!;

    public Markup? MarkupPercentage { get; private set; }

    public Guid CategoryId { get; private set; }

    public Category? Category { get; private set; }

    public ProductStatus Status { get; private set; }

    public bool IsFeatured { get; private set; }

    public Dictionary<string, string> Specifications { get; private set; } = [];

    public string? TelegramMessageId { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyList<ProductImage> Images => _images.AsReadOnly();

    public static Product Create(
        string name,
        string description,
        string brand,
        decimal basePriceUsd,
        Guid categoryId,
        Dictionary<string, string>? specifications = null)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = ValueObjects.Slug.Create(name),
            Description = description,
            Brand = brand,
            BasePriceUsd = Money.FromUsd(basePriceUsd),
            CategoryId = categoryId,
            Specifications = specifications ?? [],
            Status = ProductStatus.Draft,
            IsFeatured = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false,
        };

        product.AddDomainEvent(new ProductCreatedEvent(product.Id));
        return product;
    }

    public static Product CreateFromTelegram(
        string name,
        string description,
        string brand,
        decimal basePriceUsd,
        Guid categoryId,
        string telegramMessageId,
        Dictionary<string, string>? specifications = null)
    {
        Product product = Create(name, description, brand, basePriceUsd, categoryId, specifications);
        product.TelegramMessageId = telegramMessageId;
        return product;
    }

    public void Publish()
    {
        if (Status == ProductStatus.Deleted)
        {
            throw new DomainException("Cannot publish a deleted product");
        }

        if (_images.Count == 0)
        {
            throw new DomainException("Product must have at least one image to publish");
        }

        Status = ProductStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        Status = ProductStatus.Inactive;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SoftDelete()
    {
        Status = ProductStatus.Deleted;
        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(
        string name,
        string description,
        string brand,
        decimal basePriceUsd,
        Guid categoryId,
        Dictionary<string, string>? specifications)
    {
        Name = name;
        Slug = ValueObjects.Slug.Create(name);
        Description = description;
        Brand = brand;
        BasePriceUsd = Money.FromUsd(basePriceUsd);
        CategoryId = categoryId;
        Specifications = specifications ?? Specifications;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new ProductUpdatedEvent(Id));
    }

    public void SetMarkup(decimal? percentage)
    {
        MarkupPercentage = percentage.HasValue ? Markup.Create(percentage.Value) : null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetFeatured(bool featured)
    {
        IsFeatured = featured;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddImage(string url, int order)
    {
        if (_images.Count >= 10)
        {
            throw new DomainException("Product cannot have more than 10 images");
        }

        _images.Add(ProductImage.Create(Id, url, order));
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveImage(Guid imageId)
    {
        ProductImage image = _images.FirstOrDefault(i => i.Id == imageId)
            ?? throw new DomainException("Image not found");
        _images.Remove(image);
        UpdatedAt = DateTime.UtcNow;
    }

    public void ReorderImages(IReadOnlyList<Guid> orderedImageIds)
    {
        for (int i = 0; i < orderedImageIds.Count; i++)
        {
            ProductImage? image = _images.FirstOrDefault(img => img.Id == orderedImageIds[i]);
            image?.SetOrder(i);
        }

        UpdatedAt = DateTime.UtcNow;
    }
}
