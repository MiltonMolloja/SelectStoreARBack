using MediatR;
using Microsoft.Extensions.Logging;
using SelectStoreAR.Application.Services;
using SelectStoreAR.Domain.Entities;
using SelectStoreAR.Domain.Interfaces;

namespace SelectStoreAR.Application.Commands.Telegram;

public sealed record SyncPriceListCommand(string Text, string Source = "telegram") : IRequest<SyncPriceListResult>;

public sealed record SyncPriceListResult(
    int Created,
    int Updated,
    int Skipped,
    int Errors,
    IReadOnlyList<PriceChangeDetail> Changes);

public sealed record PriceChangeDetail(
    string ProductName,
    decimal? OldPriceUsd,
    decimal NewPriceUsd,
    string Action);

public sealed class SyncPriceListHandler(
    IProductRepository productRepository,
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork,
    ILogger<SyncPriceListHandler> logger)
    : IRequestHandler<SyncPriceListCommand, SyncPriceListResult>
{
    private sealed record ItemResult(string Action, decimal? OldPrice = null);

    public async Task<SyncPriceListResult> Handle(SyncPriceListCommand request, CancellationToken cancellationToken)
    {
        TelegramPriceListParser.PriceListResult parsed = TelegramPriceListParser.Parse(request.Text);

        if (parsed.Items.Count == 0)
        {
            logger.LogInformation("SyncPriceList: no items parsed (source={Source})", request.Source);
            return new SyncPriceListResult(0, 0, parsed.SkippedCount, 0, []);
        }

        logger.LogInformation("SyncPriceList: {Count} items parsed from {Source}", parsed.Items.Count, request.Source);

        IReadOnlyList<Category> allCategories = await categoryRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        Dictionary<string, Category> categoryByName = allCategories
            .ToDictionary(c => c.Name.ToUpperInvariant());

        int created = 0;
        int updated = 0;
        int unchanged = 0;
        int errors = 0;
        List<PriceChangeDetail> changes = [];

        foreach (TelegramPriceListParser.PriceListItem item in parsed.Items)
        {
            try
            {
                ItemResult result = await ProcessItemAsync(item, categoryByName, cancellationToken).ConfigureAwait(false);

                changes.Add(new PriceChangeDetail(item.Name, result.OldPrice, item.PriceUsd, result.Action));

                switch (result.Action)
                {
                    case "created": created++; break;
                    case "updated": updated++; break;
                    default: unchanged++; break;
                }
            }
            catch (Exception ex)
            {
                errors++;
                logger.LogError(ex, "SyncPriceList: error processing '{Name}'", item.Name);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "SyncPriceList done: created={C} updated={U} unchanged={N} errors={E}",
            created,
            updated,
            unchanged,
            errors);

        return new SyncPriceListResult(created, updated, unchanged, errors, changes);
    }

    private async Task<ItemResult> ProcessItemAsync(
        TelegramPriceListParser.PriceListItem item,
        Dictionary<string, Category> categoryByName,
        CancellationToken cancellationToken)
    {
        string slug = Domain.ValueObjects.Slug.Create(item.Name).Value;
        Product? existing = await productRepository.GetBySlugAsync(slug, cancellationToken).ConfigureAwait(false);

        if (existing is not null)
        {
            decimal oldPrice = existing.BasePriceUsd.Amount;

            if (oldPrice == item.PriceUsd)
            {
                return new ItemResult("unchanged", oldPrice);
            }

            existing.Update(existing.Name, existing.Description, existing.Brand,
                item.PriceUsd, existing.CategoryId, existing.Specifications);

            productRepository.Update(existing);
            return new ItemResult("updated", oldPrice);
        }

        Category category = await EnsureCategoryAsync(item.Category, categoryByName, cancellationToken).ConfigureAwait(false);

        Product product = Product.Create(
            item.Name,
            description: string.Empty,
            item.Brand,
            item.PriceUsd,
            category.Id);

        productRepository.Add(product);
        return new ItemResult("created");
    }

    private async Task<Category> EnsureCategoryAsync(
        string categoryName,
        Dictionary<string, Category> categoryByName,
        CancellationToken cancellationToken)
    {
        string key = categoryName.ToUpperInvariant();

        if (categoryByName.TryGetValue(key, out Category? existing))
        {
            return existing;
        }

        Category newCategory = Category.Create(categoryName);
        categoryRepository.Add(newCategory);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        categoryByName[key] = newCategory;
        return newCategory;
    }
}
