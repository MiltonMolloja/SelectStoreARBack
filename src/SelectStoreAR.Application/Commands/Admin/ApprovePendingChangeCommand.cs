using MediatR;
using Microsoft.Extensions.Logging;
using SelectStoreAR.Domain.Entities;
using SelectStoreAR.Domain.Enums;
using SelectStoreAR.Domain.Interfaces;

namespace SelectStoreAR.Application.Commands.Admin;

public sealed record ApprovePendingChangeCommand(Guid ChangeId, string ReviewedBy) : IRequest<ApprovePendingChangeResult>;

public sealed record ApprovePendingChangeResult(bool Success, string Message, Guid? ProductId = null);

public sealed class ApprovePendingChangeHandler(
    IPendingChangeRepository pendingChangeRepository,
    IProductRepository productRepository,
    ICategoryRepository categoryRepository,
    IPriceHistoryRepository priceHistoryRepository,
    IUnitOfWork unitOfWork,
    ILogger<ApprovePendingChangeHandler> logger)
    : IRequestHandler<ApprovePendingChangeCommand, ApprovePendingChangeResult>
{
    public async Task<ApprovePendingChangeResult> Handle(ApprovePendingChangeCommand request, CancellationToken cancellationToken)
    {
        ProductPendingChange? change = await pendingChangeRepository
            .GetByIdAsync(request.ChangeId, cancellationToken)
            .ConfigureAwait(false);

        if (change is null)
        {
            return new ApprovePendingChangeResult(false, "Pending change not found");
        }

        if (change.Status != PendingChangeStatus.Pending)
        {
            return new ApprovePendingChangeResult(false, $"Change is already {change.Status}");
        }

        Guid productId;

        if (change.ChangeType == PendingChangeType.Created)
        {
            // Producto nuevo — crear
            Category category = await EnsureCategoryAsync(change.ProposedCategory, cancellationToken).ConfigureAwait(false);

            Product product = Product.Create(
                change.ProposedName,
                change.ProposedDescription,
                change.ProposedBrand,
                change.ProposedPriceUsd.Amount,
                category.Id);

            product.ActivateFromImport();
            product.SetAvailability(change.ProposedAvailability);
            product.SetInspiration(change.ProposedInspiration);
            product.SetTelegramSync(change.RawTelegramText);

            productRepository.Add(product);
            productId = product.Id;

            logger.LogInformation("Approved new product: {Name} u${Price}", change.ProposedName, change.ProposedPriceUsd.Amount);
        }
        else
        {
            // Producto existente — aplicar cambio
            if (change.ProductId is null)
            {
                return new ApprovePendingChangeResult(false, "Change references no product");
            }

            Product? product = await productRepository
                .GetByIdAsync(change.ProductId.Value, cancellationToken)
                .ConfigureAwait(false);

            if (product is null)
            {
                return new ApprovePendingChangeResult(false, "Referenced product not found");
            }

            product.ApplyApprovedChange(change);
            productId = product.Id;

            logger.LogInformation(
                "Approved {Type} for {Name}: {Old} -> {New}",
                change.ChangeType,
                product.Name,
                change.CurrentPriceUsd?.Amount,
                change.ProposedPriceUsd.Amount);
        }

        // Registrar en historial de precios
        priceHistoryRepository.Add(PriceHistory.Create(
            productId,
            change.ProposedPriceUsd.Amount,
            PriceHistorySource.Approved,
            request.ReviewedBy));

        change.Approve(request.ReviewedBy);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new ApprovePendingChangeResult(true, "Approved", productId);
    }

    private async Task<Category> EnsureCategoryAsync(string categoryName, CancellationToken cancellationToken)
    {
        IReadOnlyList<Category> all = await categoryRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        Category? existing = all.FirstOrDefault(c => c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            return existing;
        }

        Category newCategory = Category.Create(categoryName);
        categoryRepository.Add(newCategory);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return newCategory;
    }
}
