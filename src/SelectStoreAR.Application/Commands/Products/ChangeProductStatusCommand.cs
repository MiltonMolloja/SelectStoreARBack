using FluentValidation;
using MediatR;
using SelectStoreAR.Domain.Common;
using SelectStoreAR.Domain.Entities;
using SelectStoreAR.Domain.Interfaces;

namespace SelectStoreAR.Application.Commands.Products;

public sealed record ChangeProductStatusCommand(Guid ProductId, string Status) : IRequest;

public sealed class ChangeProductStatusHandler(
    IProductRepository productRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ChangeProductStatusCommand>
{
    public async Task Handle(ChangeProductStatusCommand request, CancellationToken cancellationToken)
    {
        Product product = await productRepository.GetByIdAsync(request.ProductId, cancellationToken).ConfigureAwait(false)
            ?? throw new DomainException($"Producto '{request.ProductId}' no encontrado");

        switch (request.Status.ToLowerInvariant())
        {
            case "active":
                product.Publish();
                break;
            case "inactive":
                product.Deactivate();
                break;
            case "deleted":
                product.SoftDelete();
                break;
            default:
                throw new DomainException($"Estado inválido: {request.Status}. Use: active, inactive, deleted");
        }

        productRepository.Update(product);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

public sealed class ChangeProductStatusValidator : AbstractValidator<ChangeProductStatusCommand>
{
    private static readonly string[] ValidStatuses = ["active", "inactive", "deleted"];

    public ChangeProductStatusValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("El ID del producto es requerido");

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("El estado es requerido")
            .Must(s => ValidStatuses.Contains(s.ToLowerInvariant()))
            .WithMessage($"Estado inválido. Valores válidos: {string.Join(", ", ValidStatuses)}");
    }
}
