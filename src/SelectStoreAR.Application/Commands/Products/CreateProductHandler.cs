using MediatR;
using SelectStoreAR.Application.DTOs;
using SelectStoreAR.Domain.Entities;
using SelectStoreAR.Domain.Interfaces;

namespace SelectStoreAR.Application.Commands.Products;

public sealed class CreateProductHandler(
    IProductRepository productRepository,
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateProductCommand, ProductDto>
{
    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        Category? category = await categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken).ConfigureAwait(false);
        if (category is null)
        {
            throw new InvalidOperationException($"Category '{request.CategoryId}' not found");
        }

        Product product = Product.Create(
            request.Name,
            request.Description,
            request.Brand,
            request.BasePriceUsd,
            request.CategoryId,
            request.Specifications);

        if (request.MarkupPercentage.HasValue)
        {
            product.SetMarkup(request.MarkupPercentage.Value);
        }

        productRepository.Add(product);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return MapToDto(product, category);
    }

    private static ProductDto MapToDto(Product product, Category category)
    {
        return new ProductDto(
            product.Id,
            product.Name,
            product.Slug.Value,
            product.Description,
            product.Brand,
            product.BasePriceUsd.Amount,
            product.MarkupPercentage?.Percentage,
            product.BasePriceUsd.Amount,
            product.CategoryId,
            category.Name,
            category.Slug.Value,
            product.Status.ToString(),
            product.IsFeatured,
            product.Specifications,
            [],
            product.CreatedAt,
            product.UpdatedAt);
    }
}
