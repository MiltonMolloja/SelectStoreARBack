using FluentAssertions;
using SelectStoreAR.Application.Services;

namespace SelectStoreAR.Application.Tests;

public sealed class TelegramPriceListParserTests
{
    private const string CelularesMessage = """
        PIXEL IMPORTADO

        9A 128 u$490🏭
        10 128 US u$680🏭
        10 Pro 128 US u$905🏭
        """;

    private const string PerfumesMessage = """
        LATTAFA
        Asad 100ml u$26✅
        Asad Bourbon 100ml u$30✅
        Khamrah 100ml u$35✅
        His Confession 100ml u$31✅
        Hayaati 100ml u$
        """;

    private const string CamarasMessage = """
        CANON 📷

        EOS R5 Body u$2980
        EOS R5 Mark II Body u$3930
        EOS R6 Mark III Body u$2830
        PowerShot G7 X Mark III u$1626
        """;

    private const string ArmafMessage = """
        ARMAF
        Club de Nuit Sillage 105ml u$36✅
        Odyssey Mega 100ml u$37✅
        Odyssey Montagne 100ml u$55✅🆕
        Urban Man 🧔‍♂️105ml u$32✅
        """;

    [Fact]
    public void Parse_CelularesMessage_ExtractsProducts()
    {
        TelegramPriceListParser.PriceListResult result = TelegramPriceListParser.Parse(CelularesMessage);

        result.Items.Should().HaveCount(3);
        result.Items[0].PriceUsd.Should().Be(490m);
        result.Items[0].Brand.Should().Be("Pixel Importado");
        result.Items[0].AvailabilityStatus.Should().Be("warehouse");
    }

    [Fact]
    public void Parse_PerfumesMessage_ExtractsOnlyWithPrice()
    {
        TelegramPriceListParser.PriceListResult result = TelegramPriceListParser.Parse(PerfumesMessage);

        // "Hayaati 100ml u$" tiene u$ sin número → debe ignorarse
        result.Items.Should().HaveCount(4);
        result.Items.Should().NotContain(i => i.Name.Contains("Hayaati", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Parse_PerfumesMessage_DetectsBrandAndCategory()
    {
        TelegramPriceListParser.PriceListResult result = TelegramPriceListParser.Parse(PerfumesMessage);

        result.Items.Should().AllSatisfy(item =>
        {
            item.Brand.Should().Be("Lattafa");
            item.Category.Should().Be("Perfumes");
        });
    }

    [Fact]
    public void Parse_PerfumesMessage_ExtractsSize()
    {
        TelegramPriceListParser.PriceListResult result = TelegramPriceListParser.Parse(PerfumesMessage);

        result.Items[0].SizeOrVariant.Should().Be("100ML");
    }

    [Fact]
    public void Parse_CamarasMessage_DetectsCategory()
    {
        TelegramPriceListParser.PriceListResult result = TelegramPriceListParser.Parse(CamarasMessage);

        result.Items.Should().HaveCount(4);
        result.Items.Should().AllSatisfy(item =>
        {
            item.Category.Should().Be("Camaras");
            item.Brand.Should().Contain("Canon");
        });
    }

    [Fact]
    public void Parse_CamarasMessage_ExtractsCorrectPrices()
    {
        TelegramPriceListParser.PriceListResult result = TelegramPriceListParser.Parse(CamarasMessage);

        result.Items.Should().Contain(i => i.Name.Contains("EOS R5", StringComparison.OrdinalIgnoreCase)
            && i.PriceUsd == 2980m);
    }

    [Fact]
    public void Parse_ArmafMessage_ExtractsAllProductsWithAvailability()
    {
        TelegramPriceListParser.PriceListResult result = TelegramPriceListParser.Parse(ArmafMessage);

        result.Items.Should().HaveCount(4);
        result.Items.Should().AllSatisfy(item => item.AvailabilityStatus.Should().Be("available"));
    }

    [Fact]
    public void Parse_HtmlExport_CleansHtmlTags()
    {
        string htmlText = "<strong>ARMAF</strong><br><strong>Sillage</strong> 105ml u$36✅<br><strong>Mega</strong> 100ml u$37✅";

        TelegramPriceListParser.PriceListResult result = TelegramPriceListParser.Parse(htmlText);

        result.Items.Should().HaveCount(2);
        result.Items[0].Name.Should().NotContain("<");
    }

    [Fact]
    public void Parse_EmptyText_ReturnsEmptyResult()
    {
        TelegramPriceListParser.PriceListResult result = TelegramPriceListParser.Parse(string.Empty);

        result.Items.Should().BeEmpty();
        result.ParsedCount.Should().Be(0);
    }

    [Fact]
    public void Parse_InformationalMessage_SkipsNonProducts()
    {
        string text = """
            ℹ️PAGOS:
            *SOLO BILLETES DE $1000, 2000, 10.000.
            Solo en efectivo, sin depositos.
            No se toman billetes de 1 Dolar.
            """;

        TelegramPriceListParser.PriceListResult result = TelegramPriceListParser.Parse(text);

        result.Items.Should().BeEmpty();
    }

    [Fact]
    public void Parse_InspirationInParentheses_IsExtracted()
    {
        string text = """
            LATTAFA
            Asad 100ml u$26✅
            (Dior-Sauvage elixir)
            Khamrah 100ml u$35✅
            """;

        TelegramPriceListParser.PriceListResult result = TelegramPriceListParser.Parse(text);

        // "Asad" no debería tener la inspiración porque está en la línea siguiente
        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public void Parse_ProductWithPriceOnSameLine_ParsesCorrectly()
    {
        string text = """
            SAMSUNG
            A16 4G 128GB u$110✅
            A25 5G 256GB u$180✅
            """;

        TelegramPriceListParser.PriceListResult result = TelegramPriceListParser.Parse(text);

        result.Items.Should().HaveCount(2);
        result.Items[0].PriceUsd.Should().Be(110m);
        result.Items[0].Brand.Should().Be("Samsung");
        result.Items[1].PriceUsd.Should().Be(180m);
    }

    [Fact]
    public void Parse_PriceOnSeparateLine_IsSkipped()
    {
        // Precio en línea separada sin nombre → se ignora (edge case del canal)
        string text = """
            SAMSUNG A16 4G 128GB
            Nuevos - Sin Caja
            u$110✅
            """;

        TelegramPriceListParser.PriceListResult result = TelegramPriceListParser.Parse(text);

        // "u$110" en línea separada sin nombre antes del precio → skipped
        result.Items.Should().HaveCount(0);
    }
}
