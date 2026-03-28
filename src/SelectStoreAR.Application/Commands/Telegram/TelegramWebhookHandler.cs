using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SelectStoreAR.Application.Interfaces;
using SelectStoreAR.Application.Services;
using SelectStoreAR.Domain.Entities;
using SelectStoreAR.Domain.Interfaces;

namespace SelectStoreAR.Application.Commands.Telegram;

public sealed class TelegramWebhookHandler(
    IProductRepository productRepository,
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork,
    ITelegramImageService telegramImageService,
    IConfiguration configuration,
    ILogger<TelegramWebhookHandler> logger)
    : IRequestHandler<TelegramWebhookCommand, TelegramWebhookResult>
{
    public async Task<TelegramWebhookResult> Handle(TelegramWebhookCommand request, CancellationToken cancellationToken)
    {
        TelegramMessage? message = request.Update.ChannelPost ?? request.Update.EditedChannelPost;

        if (message is null)
        {
            return new TelegramWebhookResult("ok", "ignored", "No channel post in update");
        }

        // Verificar canal correcto
        long? expectedChannelId = configuration.GetValue<long?>("Telegram:ChannelId");
        if (expectedChannelId.HasValue && message.Chat.Id != expectedChannelId.Value)
        {
            return new TelegramWebhookResult("ok", "ignored", "Wrong channel");
        }

        // Parsear mensaje
        ParsedTelegramProduct? parsed;
        try
        {
            parsed = TelegramMessageParser.Parse(message);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning("Failed to parse Telegram message {MessageId}: {Error}", message.MessageId, ex.Message);
            return new TelegramWebhookResult("ok", "error", ex.Message);
        }

        if (parsed is null)
        {
            return new TelegramWebhookResult("ok", "ignored", "No #importar hashtag found");
        }

        string telegramMsgId = message.MessageId.ToString(System.Globalization.CultureInfo.InvariantCulture);

        // Verificar si ya importamos este mensaje (protección contra re-delivery)
        if (await productRepository.TelegramMessageIdExistsAsync(telegramMsgId, cancellationToken).ConfigureAwait(false))
        {
            logger.LogInformation("Telegram message {MessageId} already imported, skipping", telegramMsgId);
            return new TelegramWebhookResult("ok", "duplicate", $"Message {telegramMsgId} already imported");
        }

        // Buscar o crear categoría
        Category category = await FindOrCreateCategoryAsync(parsed.Category, cancellationToken).ConfigureAwait(false);

        // Crear producto nuevo (borrador)
        Product product = Product.CreateFromTelegram(
            parsed.Name,
            parsed.Description,
            parsed.Brand,
            parsed.PriceUsd,
            category.Id,
            telegramMsgId);

        productRepository.Add(product);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Descargar imágenes — errores de imagen no detienen la operación
        for (int i = 0; i < parsed.PhotoFileIds.Count; i++)
        {
            await TryDownloadImageAsync(parsed.PhotoFileIds[i], product, i, cancellationToken)
                .ConfigureAwait(false);
        }

        productRepository.Update(product);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Telegram product imported: {ProductName} ({ProductId})", product.Name, product.Id);

        return new TelegramWebhookResult("ok", "product_imported", null, product.Id, product.Name);
    }

    private async Task TryDownloadImageAsync(string fileId, Product product, int index, CancellationToken cancellationToken)
    {
        try
        {
            string imageUrl = await telegramImageService
                .DownloadAndSaveAsync(fileId, product.Id, index, cancellationToken)
                .ConfigureAwait(false);

            product.AddImage(imageUrl, index);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP error downloading Telegram image {Index} for product {ProductId}", index, product.Id);
        }
        catch (IOException ex)
        {
            logger.LogError(ex, "IO error saving Telegram image {Index} for product {ProductId}", index, product.Id);
        }
    }

    private async Task<Category> FindOrCreateCategoryAsync(string categoryName, CancellationToken cancellationToken)
    {
        IReadOnlyList<Category> allCategories = await categoryRepository
            .GetAllAsync(cancellationToken)
            .ConfigureAwait(false);

        Category? found = allCategories
            .FirstOrDefault(c => c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));

        if (found is not null)
        {
            return found;
        }

        Category newCategory = Category.Create(categoryName);
        categoryRepository.Add(newCategory);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return newCategory;
    }
}
