using MediatR;
using Microsoft.Extensions.Logging;
using SelectStoreAR.Domain.Entities;
using SelectStoreAR.Domain.Enums;
using SelectStoreAR.Domain.Interfaces;

namespace SelectStoreAR.Application.Commands.Orders;

/// <summary>
/// Registra el precio de un producto en el historial al enviar cotización por WhatsApp.
/// </summary>
public sealed record RecordWhatsAppQuoteCommand(
    Guid ProductId,
    Guid? OrderId,
    string? QuotedBy) : IRequest<RecordWhatsAppQuoteResult>;

public sealed record RecordWhatsAppQuoteResult(bool Success, string Message);

public sealed class RecordWhatsAppQuoteHandler(
    IProductRepository productRepository,
    IPriceHistoryRepository priceHistoryRepository,
    IUnitOfWork unitOfWork,
    ILogger<RecordWhatsAppQuoteHandler> logger)
    : IRequestHandler<RecordWhatsAppQuoteCommand, RecordWhatsAppQuoteResult>
{
    public async Task<RecordWhatsAppQuoteResult> Handle(RecordWhatsAppQuoteCommand request, CancellationToken cancellationToken)
    {
        Product? product = await productRepository
            .GetByIdAsync(request.ProductId, cancellationToken)
            .ConfigureAwait(false);

        if (product is null)
        {
            return new RecordWhatsAppQuoteResult(false, "Product not found");
        }

        priceHistoryRepository.Add(PriceHistory.Create(
            product.Id,
            product.BasePriceUsd.Amount,
            PriceHistorySource.WhatsAppQuote,
            request.QuotedBy,
            request.OrderId));

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "WhatsApp quote recorded: {Product} at u${Price} by {By}",
            product.Name,
            product.BasePriceUsd.Amount,
            request.QuotedBy);

        return new RecordWhatsAppQuoteResult(true, "Quote recorded");
    }
}
