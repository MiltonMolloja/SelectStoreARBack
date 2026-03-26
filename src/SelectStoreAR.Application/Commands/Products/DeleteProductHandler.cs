using MediatR;
using SelectStoreAR.Domain.Common;
using SelectStoreAR.Domain.Entities;
using SelectStoreAR.Domain.Interfaces;

namespace SelectStoreAR.Application.Commands.Products;

public sealed class DeleteProductHandler(
    IProductRepository productRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteProductCommand>
{
    public async Task Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        Product product = await productRepository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new DomainException($"Product '{request.Id}' not found");

        product.SoftDelete();
        productRepository.Update(product);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
