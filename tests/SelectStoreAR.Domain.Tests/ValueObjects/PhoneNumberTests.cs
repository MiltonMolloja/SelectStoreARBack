using FluentAssertions;
using SelectStoreAR.Domain.Common;
using SelectStoreAR.Domain.ValueObjects;

namespace SelectStoreAR.Domain.Tests.ValueObjects;

public sealed class PhoneNumberTests
{
    [Theory]
    [InlineData("+5493881234567")]
    [InlineData("5493881234567")]
    [InlineData("3881234567")]
    public void Create_WithValidPhone_Succeeds(string phone)
    {
        Action act = () => PhoneNumber.Create(phone);

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("not-a-phone")]
    [InlineData("123")]
    [InlineData("")]
    public void Create_WithInvalidPhone_ThrowsDomainException(string phone)
    {
        Action act = () => PhoneNumber.Create(phone);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_WithoutPlus_AddsArgentineCountryCode()
    {
        PhoneNumber phone = PhoneNumber.Create("3881234567");

        phone.Value.Should().StartWith("+54");
    }

    [Fact]
    public void FormatForWhatsApp_RemovesPlusSign()
    {
        PhoneNumber phone = PhoneNumber.Create("+5493881234567");

        string result = phone.FormatForWhatsApp();

        result.Should().Be("5493881234567");
    }
}
