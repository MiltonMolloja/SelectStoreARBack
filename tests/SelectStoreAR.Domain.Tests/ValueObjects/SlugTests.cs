using FluentAssertions;
using SelectStoreAR.Domain.Common;
using SelectStoreAR.Domain.ValueObjects;

namespace SelectStoreAR.Domain.Tests.ValueObjects;

public sealed class SlugTests
{
    [Theory]
    [InlineData("Samsung Galaxy S26 Ultra", "samsung-galaxy-s26-ultra")]
    [InlineData("AirPods Pro (3ª Generación)", "airpods-pro-3a-generacion")]
    [InlineData("Dior Sauvage EDP 100ml", "dior-sauvage-edp-100ml")]
    [InlineData("iPhone 17 Pro Max", "iphone-17-pro-max")]
    [InlineData("  Producto con espacios  ", "producto-con-espacios")]
    public void Create_WithValidText_GeneratesExpectedSlug(string input, string expected)
    {
        Slug slug = Slug.Create(input);

        slug.Value.Should().Be(expected);
    }

    [Theory]
    [InlineData("Celulares", "celulares")]
    [InlineData("Televisión y Video", "television-y-video")]
    [InlineData("Año nuevo 2026", "ano-nuevo-2026")]
    public void Create_WithSpanishChars_NormalizesCorrectly(string input, string expected)
    {
        Slug slug = Slug.Create(input);

        slug.Value.Should().Be(expected);
    }

    [Fact]
    public void Create_WithEmptyString_ThrowsDomainException()
    {
        Action act = () => Slug.Create(string.Empty);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_WithWhitespaceOnly_ThrowsDomainException()
    {
        Action act = () => Slug.Create("   ");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void ImplicitConversion_ToString_ReturnsValue()
    {
        Slug slug = Slug.Create("Test Product");
        string value = slug;

        value.Should().Be("test-product");
    }

    [Fact]
    public void Equality_SameSlugs_AreEqual()
    {
        Slug slug1 = Slug.Create("Samsung Galaxy");
        Slug slug2 = Slug.Create("Samsung Galaxy");

        slug1.Should().Be(slug2);
    }
}
