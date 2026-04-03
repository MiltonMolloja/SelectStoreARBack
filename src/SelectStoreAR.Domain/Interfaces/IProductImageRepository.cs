using SelectStoreAR.Domain.Entities;

namespace SelectStoreAR.Domain.Interfaces;

public interface IProductImageRepository
{
    void Add(ProductImage image);

    void AddRange(IEnumerable<ProductImage> images);

    void Remove(ProductImage image);
}
