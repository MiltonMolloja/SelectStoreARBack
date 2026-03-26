using FluentAssertions;
using SelectStoreAR.Domain.Common;
using SelectStoreAR.Domain.Entities;
using SelectStoreAR.Domain.Enums;
using SelectStoreAR.Domain.Events;

namespace SelectStoreAR.Domain.Tests.Entities;

public sealed class ProductTests
{
    private static readonly Guid CategoryId = Guid.NewGuid();

    private static Product CreateProduct(string name = "Samsung Galaxy S26 Ultra")
    {
        return Product.Create(name, "Description", "Samsung", 1000m, CategoryId);
    }

    [Fact]
    public void Create_WithValidData_CreatesProductAsDraft()
    {
        Product product = CreateProduct();

        product.Status.Should().Be(ProductStatus.Draft);
        product.IsFeatured.Should().BeFalse();
        product.IsDeleted.Should().BeFalse();
        product.Images.Should().BeEmpty();
    }

    [Fact]
    public void Create_GeneratesSlugFromName()
    {
        Product product = CreateProduct("Samsung Galaxy S26 Ultra");

        product.Slug.Value.Should().Be("samsung-galaxy-s26-ultra");
    }

    [Fact]
    public void Create_RaisesProductCreatedEvent()
    {
        Product product = CreateProduct();

        product.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ProductCreatedEvent>();
    }

    [Fact]
    public void Publish_WithoutImages_ThrowsDomainException()
    {
        Product product = CreateProduct();

        Action act = () => product.Publish();

        act.Should().Throw<DomainException>()
            .WithMessage("*at least one image*");
    }

    [Fact]
    public void Publish_WithImages_ChangesStatusToActive()
    {
        Product product = CreateProduct();
        product.AddImage("/images/products/test/original-1.webp", 0);

        product.Publish();

        product.Status.Should().Be(ProductStatus.Active);
    }

    [Fact]
    public void AddImage_ExceedsMaximum_ThrowsDomainException()
    {
        Product product = CreateProduct();
        for (int i = 0; i < 10; i++)
        {
            product.AddImage($"/images/products/test/original-{i}.webp", i);
        }

        Action act = () => product.AddImage("/images/products/test/original-10.webp", 10);

        act.Should().Throw<DomainException>()
            .WithMessage("*more than 10 images*");
    }

    [Fact]
    public void SoftDelete_SetsIsDeletedAndChangesStatus()
    {
        Product product = CreateProduct();

        product.SoftDelete();

        product.IsDeleted.Should().BeTrue();
        product.Status.Should().Be(ProductStatus.Deleted);
    }

    [Fact]
    public void SetMarkup_WithValidPercentage_UpdatesMarkup()
    {
        Product product = CreateProduct();

        product.SetMarkup(30m);

        product.MarkupPercentage.Should().NotBeNull();
        product.MarkupPercentage!.Percentage.Should().Be(30m);
    }

    [Fact]
    public void SetMarkup_WithNull_ClearsMarkup()
    {
        Product product = CreateProduct();
        product.SetMarkup(25m);

        product.SetMarkup(null);

        product.MarkupPercentage.Should().BeNull();
    }

    [Fact]
    public void Update_RaisesProductUpdatedEvent()
    {
        Product product = CreateProduct();
        product.ClearDomainEvents();

        product.Update("New Name", "New description", "Samsung", 1100m, CategoryId, null);

        product.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ProductUpdatedEvent>();
    }

    [Fact]
    public void ReorderImages_ChangesImageOrder()
    {
        Product product = CreateProduct();
        product.AddImage("/images/products/test/original-0.webp", 0);
        product.AddImage("/images/products/test/original-1.webp", 1);

        Guid firstId = product.Images[0].Id;
        Guid secondId = product.Images[1].Id;

        product.ReorderImages([secondId, firstId]);

        product.Images.First(i => i.Id == secondId).SortOrder.Should().Be(0);
        product.Images.First(i => i.Id == firstId).SortOrder.Should().Be(1);
    }
}
