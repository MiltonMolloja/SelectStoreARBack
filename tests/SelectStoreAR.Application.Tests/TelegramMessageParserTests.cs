using FluentAssertions;
using SelectStoreAR.Application.Commands.Telegram;
using SelectStoreAR.Application.Services;

namespace SelectStoreAR.Domain.Tests.Services;

public sealed class TelegramMessageParserTests
{
    private static TelegramMessage CreateMessage(string text, IReadOnlyList<TelegramPhotoSize>? photos = null)
    {
        return new TelegramMessage(
            MessageId: 789,
            Chat: new TelegramChat(-1001234567890, "Test Channel", "channel"),
            Date: 1711123200,
            Text: text,
            Caption: null,
            Photo: photos);
    }

    private const string ValidMessage = """
        📦 Samsung Galaxy S26 Ultra 256GB
        💰 1250
        📁 Celulares
        🏷️ Samsung
        📝 Ultimo modelo, pantalla 6.9", camara 200MP.

        #importar
        """;

    [Fact]
    public void Parse_WithValidFormat_ExtractsAllFields()
    {
        TelegramMessage message = CreateMessage(ValidMessage);

        ParsedTelegramProduct? result = TelegramMessageParser.Parse(message);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Samsung Galaxy S26 Ultra 256GB");
        result.PriceUsd.Should().Be(1250m);
        result.Category.Should().Be("Celulares");
        result.Brand.Should().Be("Samsung");
    }

    [Fact]
    public void Parse_WithoutImportarHashtag_ReturnsNull()
    {
        TelegramMessage message = CreateMessage("Un mensaje sin el hashtag requerido");

        ParsedTelegramProduct? result = TelegramMessageParser.Parse(message);

        result.Should().BeNull();
    }

    [Fact]
    public void Parse_WithMissingName_ThrowsInvalidOperation()
    {
        string text = """
            💰 1250
            📁 Celulares
            🏷️ Samsung
            #importar
            """;

        TelegramMessage message = CreateMessage(text);

        Action act = () => TelegramMessageParser.Parse(message);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*📦*");
    }

    [Fact]
    public void Parse_WithMissingPrice_ThrowsInvalidOperation()
    {
        string text = """
            📦 Samsung Galaxy
            📁 Celulares
            🏷️ Samsung
            #importar
            """;

        TelegramMessage message = CreateMessage(text);

        Action act = () => TelegramMessageParser.Parse(message);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*💰*");
    }

    [Fact]
    public void Parse_WithInvalidPrice_ThrowsInvalidOperation()
    {
        string text = """
            📦 Samsung Galaxy
            💰 invalid-price
            📁 Celulares
            🏷️ Samsung
            #importar
            """;

        TelegramMessage message = CreateMessage(text);

        Action act = () => TelegramMessageParser.Parse(message);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Parse_WithPhotos_ExtractsHighestResolutionPerGroup()
    {
        List<TelegramPhotoSize> photos =
        [
            new TelegramPhotoSize("file_low", "unique1", 320, 240, 10000),
            new TelegramPhotoSize("file_high", "unique1", 1280, 960, 150000),
            new TelegramPhotoSize("file_single", "unique2", 800, 600, 80000),
        ];

        TelegramMessage message = CreateMessage(ValidMessage, photos);

        ParsedTelegramProduct? result = TelegramMessageParser.Parse(message);

        result!.PhotoFileIds.Should().HaveCount(2);
        result.PhotoFileIds.Should().Contain("file_high");
        result.PhotoFileIds.Should().Contain("file_single");
    }

    [Fact]
    public void Parse_WithNullText_ReturnsNull()
    {
        TelegramMessage message = new(789, new TelegramChat(-100, "Test", "channel"), 123, null, null, null);

        ParsedTelegramProduct? result = TelegramMessageParser.Parse(message);

        result.Should().BeNull();
    }
}
