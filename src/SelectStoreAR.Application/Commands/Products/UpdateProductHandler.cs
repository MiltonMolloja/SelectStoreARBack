using MediatR;
using SelectStoreAR.Application.DTOs;
using SelectStoreAR.Domain.Common;
using SelectStoreAR.Domain.Entities;
using SelectStoreAR.Domain.Interfaces;

namespace SelectStoreAR.Application.Commands.Products;

public sealed class UpdateProductHandler(
    IProductRepository productRepository,
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateProductCommand, ProductDto>
{
    public async Task<ProductDto> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        Product product = await productRepository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new DomainException($"Product '{request.Id}' not found");

        Category? category = await categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken).ConfigureAwait(false);
        if (category is null)
        {
            throw new InvalidOperationException($"Category '{request.CategoryId}' not found");
        }

        product.Update(
            request.Name,
            request.Description,
            request.Brand,
            request.BasePriceUsd,
            request.CategoryId,
            request.Specifications);

        product.SetMarkup(request.MarkupPercentage);
        product.SetFeatured(request.IsFeatured);

        productRepository.Update(product);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

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
            product.Images.Select(i => new ProductImageDto(i.Id, i.Url, i.ThumbnailUrl, i.MediumUrl, i.AltText, i.SortOrder)).ToList(),
            product.CreatedAt,
            product.UpdatedAt);
    }
}
