using System.Globalization;
using SelectStoreAR.Domain.Common;
using SelectStoreAR.Domain.Enums;

namespace SelectStoreAR.Domain.ValueObjects;

public sealed record Money
{
    private Money(decimal amount, Currency currency)
    {
        if (amount < 0)
        {
            throw new DomainException("Money amount cannot be negative");
        }

        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; }

    public Currency Currency { get; }

    public static Money FromUsd(decimal amount) => new(amount, Currency.USD);

    public static Money FromArs(decimal amount) => new(amount, Currency.ARS);

    public Money ConvertTo(Currency target, decimal exchangeRate)
    {
        if (Currency == target)
        {
            return this;
        }

        return target switch
        {
            Currency.ARS => FromArs(Math.Round(Amount * exchangeRate, 0)),
            Currency.USD => FromUsd(Math.Round(Amount / exchangeRate, 2)),
            _ => throw new DomainException($"Unsupported currency: {target}"),
        };
    }

    public Money ApplyMarkup(Markup markup)
    {
        decimal finalAmount = Amount * (1 + (markup.Percentage / 100));
        return new Money(Math.Round(finalAmount, 2, MidpointRounding.AwayFromZero), Currency);
    }

    public string Format() => Currency switch
    {
        Currency.USD => $"US$ {Amount.ToString("N2", CultureInfo.InvariantCulture)}",
        Currency.ARS => $"$ {Amount.ToString("N0", CultureInfo.GetCultureInfo("es-AR"))}",
        _ => Amount.ToString("N2", CultureInfo.InvariantCulture),
    };

    public override string ToString() => Format();
}
