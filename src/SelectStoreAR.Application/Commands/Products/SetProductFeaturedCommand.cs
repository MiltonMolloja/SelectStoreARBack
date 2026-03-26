using MediatR;
using SelectStoreAR.Domain.Common;
using SelectStoreAR.Domain.Entities;
using SelectStoreAR.Domain.Interfaces;

namespace SelectStoreAR.Application.Commands.Products;

public sealed record SetProductFeaturedCommand(Guid ProductId, bool IsFeatured) : IRequest;

public sealed class SetProductFeaturedHandler(
    IProductRepository productRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<SetProductFeaturedCommand>
{
    public async Task Handle(SetProductFeaturedCommand request, CancellationToken cancellationToken)
    {
        Product product = await productRepository.GetByIdAsync(request.ProductId, cancellationToken).ConfigureAwait(false)
            ?? throw new DomainException($"Producto '{request.ProductId}' no encontrado");

        product.SetFeatured(request.IsFeatured);
        productRepository.Update(product);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
