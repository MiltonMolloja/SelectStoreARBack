using SelectStoreAR.Domain.Common;
using SelectStoreAR.Domain.ValueObjects;

namespace SelectStoreAR.Domain.Entities;

public sealed class Category : BaseEntity
{
    private readonly List<Category> _children = [];
    private readonly List<Product> _products = [];

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public Slug Slug { get; private set; } = null!;

    public Guid? ParentId { get; private set; }

    public Category? Parent { get; private set; }

    public Markup? DefaultMarkup { get; private set; }

    public int SortOrder { get; private set; }

    public string? ImageUrl { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyList<Category> Children => _children.AsReadOnly();

    public IReadOnlyList<Product> Products => _products.AsReadOnly();

    private Category()
    {
    }

    public static Category Create(string name, Guid? parentId = null, int sortOrder = 0)
    {
        return new Category
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = ValueObjects.Slug.Create(name),
            ParentId = parentId,
            SortOrder = sortOrder,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    public void Update(string name, Guid? parentId, int sortOrder)
    {
        if (parentId == Id)
        {
            throw new DomainException("Category cannot be its own parent");
        }

        Name = name;
        Slug = ValueObjects.Slug.Create(name);
        ParentId = parentId;
        SortOrder = sortOrder;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetDefaultMarkup(decimal? percentage)
    {
        DefaultMarkup = percentage.HasValue ? Markup.Create(percentage.Value) : null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetImage(string? imageUrl)
    {
        ImageUrl = imageUrl;
        UpdatedAt = DateTime.UtcNow;
    }
}
