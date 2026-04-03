using SelectStoreAR.Domain.Entities;
using SelectStoreAR.Domain.Interfaces;

namespace SelectStoreAR.Infrastructure.Persistence.Repositories;

public sealed class ProductImageRepository(AppDbContext dbContext) : IProductImageRepository
{
    public void Add(ProductImage image) => dbContext.ProductImages.Add(image);

    public void AddRange(IEnumerable<ProductImage> images) => dbContext.ProductImages.AddRange(images);

    public void Remove(ProductImage image) => dbContext.ProductImages.Remove(image);
}
