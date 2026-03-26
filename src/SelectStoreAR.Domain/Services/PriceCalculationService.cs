using SelectStoreAR.Domain.Entities;
using SelectStoreAR.Domain.Enums;
using SelectStoreAR.Domain.ValueObjects;

namespace SelectStoreAR.Domain.Services;

public sealed class PriceCalculationService
{
    public static Money CalculateFinalPrice(
        Product product,
        Category category,
        decimal globalMarkupPercentage,
        Currency targetCurrency,
        decimal exchangeRate)
    {
        Markup markup = product.MarkupPercentage
            ?? category.DefaultMarkup
            ?? Markup.Create(globalMarkupPercentage);

        Money finalPriceUsd = product.BasePriceUsd.ApplyMarkup(markup);

        return targetCurrency switch
        {
            Currency.USD => finalPriceUsd,
            Currency.ARS => finalPriceUsd.ConvertTo(Currency.ARS, exchangeRate),
            _ => finalPriceUsd,
        };
    }
}
