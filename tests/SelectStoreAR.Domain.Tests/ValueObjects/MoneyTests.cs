using FluentAssertions;
using SelectStoreAR.Domain.Common;
using SelectStoreAR.Domain.Enums;
using SelectStoreAR.Domain.ValueObjects;

namespace SelectStoreAR.Domain.Tests.ValueObjects;

public sealed class MoneyTests
{
    [Fact]
    public void FromUsd_WithValidAmount_CreatesCorrectly()
    {
        Money money = Money.FromUsd(100.50m);

        money.Amount.Should().Be(100.50m);
        money.Currency.Should().Be(Currency.USD);
    }

    [Fact]
    public void FromArs_WithValidAmount_CreatesCorrectly()
    {
        Money money = Money.FromArs(150000m);

        money.Amount.Should().Be(150000m);
        money.Currency.Should().Be(Currency.ARS);
    }

    [Fact]
    public void FromUsd_WithNegativeAmount_ThrowsDomainException()
    {
        Action act = () => Money.FromUsd(-1m);

        act.Should().Throw<DomainException>()
            .WithMessage("*negative*");
    }

    [Fact]
    public void ConvertTo_SameCurrency_ReturnsSelf()
    {
        Money money = Money.FromUsd(100m);
        Money result = money.ConvertTo(Currency.USD, 1250m);

        result.Should().Be(money);
    }

    [Fact]
    public void ConvertTo_UsdToArs_MultipliesByRate()
    {
        Money money = Money.FromUsd(100m);
        Money result = money.ConvertTo(Currency.ARS, 1250m);

        result.Amount.Should().Be(125000m);
        result.Currency.Should().Be(Currency.ARS);
    }

    [Fact]
    public void ApplyMarkup_25Percent_IncreasesCorrectly()
    {
        Money money = Money.FromUsd(1000m);
        Markup markup = Markup.Create(25m);

        Money result = money.ApplyMarkup(markup);

        result.Amount.Should().Be(1250m);
        result.Currency.Should().Be(Currency.USD);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(100, 125)]
    [InlineData(1000, 1250)]
    [InlineData(850.50, 1063.13)]
    public void ApplyMarkup_25Percent_ProducesExpectedResult(decimal baseAmount, decimal expectedResult)
    {
        Money money = Money.FromUsd(baseAmount);
        Markup markup = Markup.Create(25m);

        Money result = money.ApplyMarkup(markup);

        result.Amount.Should().Be(expectedResult);
    }
}
