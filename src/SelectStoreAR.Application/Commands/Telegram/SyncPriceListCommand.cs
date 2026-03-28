using MediatR;
using Microsoft.Extensions.Logging;
using SelectStoreAR.Application.Interfaces;
using SelectStoreAR.Application.Services;
using SelectStoreAR.Domain.Entities;
using SelectStoreAR.Domain.Enums;
using SelectStoreAR.Domain.Interfaces;

namespace SelectStoreAR.Application.Commands.Telegram;

public sealed record SyncPriceListCommand(string Text, string Source = "telegram") : IRequest<SyncPriceListResult>;

public sealed record SyncPriceListResult(
    int Created,
    int Updated,
    int Skipped,
    int Errors,
    IReadOnlyList<PriceChangeDetail> Changes,
    IReadOnlyList<string> ErrorMessages);

public sealed record PriceChangeDetail(
    string ProductName,
    decimal? OldPriceUsd,
    decimal NewPriceUsd,
    string Action);

/// <summary>
/// Procesa texto de Telegram y crea ProductPendingChange en lugar de modificar productos directamente.
/// El admin debe aprobar los cambios antes de que se apliquen.
/// </summary>
public sealed class SyncPriceListHandler(
    IProductRepository productRepository,
    IPendingChangeRepository pendingChangeRepository,
    IUnitOfWork unitOfWork,
    INotificationService notificationService,
    ILogger<SyncPriceListHandler> logger)
    : IRequestHandler<SyncPriceListCommand, SyncPriceListResult>
{
    private static string StripAllSurrogates(string text) =>
        System.Text.RegularExpressions.Regex.Replace(text, @"[\uD800-\uDFFF]", string.Empty);

    private static string StripOrphanSurrogates(string text) =>
        System.Text.RegularExpressions.Regex.Replace(
            text,
            @"(?<![\uD800-\uDBFF])[\uDC00-\uDFFF]|[\uD800-\uDBFF](?![\uDC00-\uDFFF])",
            string.Empty);

    public async Task<SyncPriceListResult> Handle(SyncPriceListCommand request, CancellationToken cancellationToken)
    {
        TelegramPriceListParser.PriceListResult parsed = TelegramPriceListParser.Parse(StripOrphanSurrogates(request.Text));

        if (parsed.Items.Count == 0)
        {
            logger.LogInformation("SyncPriceList: no items parsed (source={Source})", request.Source);
            return new SyncPriceListResult(0, 0, parsed.SkippedCount, 0, [], []);
        }

        logger.LogInformation("SyncPriceList: {Count} items parsed from {Source}", parsed.Items.Count, request.Source);

        Guid batchId = Guid.NewGuid();
        int created = 0;
        int updated = 0;
        int unchanged = 0;
        int errors = 0;
        List<PriceChangeDetail> changes = [];
        List<string> errorMessages = [];

        foreach (TelegramPriceListParser.PriceListItem item in parsed.Items)
        {
            try
            {
                ProcessResult result = await ProcessItemAsync(item, batchId, cancellationToken).ConfigureAwait(false);
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                changes.Add(new PriceChangeDetail(item.Name, result.OldPrice, item.PriceUsd, result.Action));

                switch (result.Action)
                {
                    case "pending_new":
                        created++;
                        break;
                    case "pending_update":
                        updated++;
                        break;
                    default:
                        unchanged++;
                        break;
                }
            }
            catch (Exception ex)
            {
                errors++;
                errorMessages.Add($"{item.Name}: {ex.GetType().Name}: {ex.InnerException?.Message ?? ex.Message}");
                logger.LogError(ex, "SyncPriceList: error processing '{Name}'", item.Name);
            }
        }

        logger.LogInformation(
            "SyncPriceList done: pending_new={C} pending_update={U} unchanged={N} errors={E} batchId={B}",
            created,
            updated,
            unchanged,
            errors,
            batchId);

        // Notificar al admin si hay cambios pendientes
        if (created > 0 || updated > 0)
        {
            try
            {
                await notificationService.NotifyPendingBatchAsync(batchId, created, updated, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "SyncPriceList: failed to send notification for batch {BatchId}", batchId);
            }
        }

        return new SyncPriceListResult(created, updated, unchanged, errors, changes, errorMessages);
    }

    private sealed record ProcessResult(string Action, decimal? OldPrice = null);

    private async Task<ProcessResult> ProcessItemAsync(
        TelegramPriceListParser.PriceListItem item,
        Guid batchId,
        CancellationToken cancellationToken)
    {
        string cleanName = StripAllSurrogates(item.Name).Trim();
        string cleanBrand = StripAllSurrogates(item.Brand).Trim();
        string cleanRawLine = StripAllSurrogates(item.RawLine).Trim();

        if (string.IsNullOrWhiteSpace(cleanName))
        {
            return new ProcessResult("unchanged");
        }

        string slug = Domain.ValueObjects.Slug.Create(cleanName).Value;
        Product? existing = await productRepository.GetBySlugAsync(slug, cancellationToken).ConfigureAwait(false);
        AvailabilityStatus availability = ParseAvailability(item.AvailabilityStatus);

        if (existing is not null)
        {
            // Producto existente — actualizar last_telegram_raw siempre
            existing.SetTelegramSync(cleanRawLine);

            decimal oldPrice = existing.BasePriceUsd.Amount;
            bool priceChanged = oldPrice != item.PriceUsd;
            bool availabilityChanged = existing.Availability != availability;
            bool inspirationChanged = existing.Inspiration != item.Inspiration;

            if (!priceChanged && !availabilityChanged && !inspirationChanged)
            {
                return new ProcessResult("unchanged", oldPrice);
            }

            // Determinar tipo de cambio
            PendingChangeType changeType = priceChanged
                ? PendingChangeType.PriceChanged
                : PendingChangeType.AvailabilityChanged;

            // ¿Ya existe un pending para este producto? → reemplazar
            ProductPendingChange? existingPending = await pendingChangeRepository
                .GetPendingByProductIdAsync(existing.Id, cancellationToken)
                .ConfigureAwait(false);

            if (existingPending is not null)
            {
                existingPending.ReplaceWith(
                    batchId,
                    telegramMessageId: null,
                    cleanRawLine,
                    item.PriceUsd,
                    availability,
                    item.Inspiration,
                    changeType);
            }
            else
            {
                ProductPendingChange pending = ProductPendingChange.CreateUpdate(
                    existing.Id,
                    batchId,
                    telegramMessageId: null,
                    cleanRawLine,
                    cleanName,
                    cleanBrand,
                    item.PriceUsd,
                    availability,
                    item.Inspiration,
                    item.Category,
                    oldPrice,
                    changeType);

                pendingChangeRepository.Add(pending);
            }

            return new ProcessResult("pending_update", oldPrice);
        }

        // Producto nuevo — ¿ya existe un pending con el mismo nombre?
        ProductPendingChange? existingNewPending = await pendingChangeRepository
            .GetPendingByProposedNameAsync(cleanName, cancellationToken)
            .ConfigureAwait(false);

        if (existingNewPending is not null)
        {
            existingNewPending.ReplaceWith(
                batchId,
                telegramMessageId: null,
                cleanRawLine,
                item.PriceUsd,
                availability,
                item.Inspiration,
                PendingChangeType.Created);

            return new ProcessResult("pending_new");
        }

        ProductPendingChange newPending = ProductPendingChange.CreateNew(
            batchId,
            telegramMessageId: null,
            cleanRawLine,
            cleanName,
            cleanBrand,
            item.PriceUsd,
            availability,
            item.Inspiration,
            item.Category);

        pendingChangeRepository.Add(newPending);
        return new ProcessResult("pending_new");
    }

    private static AvailabilityStatus ParseAvailability(string status) => status switch
    {
        "available" => AvailabilityStatus.Available,
        "warehouse" => AvailabilityStatus.Warehouse,
        "arriving" => AvailabilityStatus.Arriving,
        "on_demand" => AvailabilityStatus.OnDemand,
        _ => AvailabilityStatus.Unknown,
    };
}
