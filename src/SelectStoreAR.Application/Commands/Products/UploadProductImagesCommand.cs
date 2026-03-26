using MediatR;
using SelectStoreAR.Application.DTOs;
using SelectStoreAR.Application.Interfaces;
using SelectStoreAR.Domain.Common;
using SelectStoreAR.Domain.Entities;
using SelectStoreAR.Domain.Interfaces;

namespace SelectStoreAR.Application.Commands.Products;

public sealed record UploadProductImagesCommand(
    Guid ProductId,
    IReadOnlyList<(Stream Stream, string FileName)> Files) : IRequest<IReadOnlyList<ProductImageDto>>;

public sealed class UploadProductImagesHandler(
    IProductRepository productRepository,
    IImageService imageService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UploadProductImagesCommand, IReadOnlyList<ProductImageDto>>
{
    public async Task<IReadOnlyList<ProductImageDto>> Handle(
        UploadProductImagesCommand request,
        CancellationToken cancellationToken)
    {
        Product product = await productRepository.GetByIdAsync(request.ProductId, cancellationToken).ConfigureAwait(false)
            ?? throw new DomainException($"Product '{request.ProductId}' not found");

        if (product.Images.Count + request.Files.Count > 10)
        {
            throw new DomainException($"Product cannot have more than 10 images. Current: {product.Images.Count}, Adding: {request.Files.Count}");
        }

        List<ProductImageDto> results = [];
        int currentOrder = product.Images.Count;

        foreach ((Stream stream, string fileName) in request.Files)
        {
            string url = await imageService.SaveImageAsync(stream, fileName, request.ProductId, cancellationToken).ConfigureAwait(false);
            product.AddImage(url, currentOrder++);
        }

        productRepository.Update(product);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        results.AddRange(product.Images
            .OrderBy(i => i.SortOrder)
            .Select(i => new ProductImageDto(i.Id, i.Url, i.ThumbnailUrl, i.MediumUrl, i.AltText, i.SortOrder)));

        return results;
    }
}
