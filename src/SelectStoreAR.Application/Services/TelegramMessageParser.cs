using System.Text.RegularExpressions;
using SelectStoreAR.Application.Commands.Telegram;

namespace SelectStoreAR.Application.Services;

public sealed record ParsedTelegramProduct(
    string Name,
    decimal PriceUsd,
    string Category,
    string Brand,
    string Description,
    IReadOnlyList<string> PhotoFileIds);

public static class TelegramMessageParser
{
    public static ParsedTelegramProduct? Parse(TelegramMessage message)
    {
        string? text = message.Caption ?? message.Text;
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        if (!text.Contains("#importar", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        string? name = ExtractField(text, "📦");
        string? priceStr = ExtractField(text, "💰");
        string? category = ExtractField(text, "📁");
        string? brand = ExtractField(text, "🏷️");
        string? description = ExtractDescription(text, "📝");

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Missing product name (📦)");
        }

        if (string.IsNullOrWhiteSpace(priceStr) || !decimal.TryParse(priceStr, out decimal price) || price <= 0)
        {
            throw new InvalidOperationException("Missing or invalid price (💰)");
        }

        if (string.IsNullOrWhiteSpace(category))
        {
            throw new InvalidOperationException("Missing category (📁)");
        }

        IReadOnlyList<string> photoFileIds = message.Photo is null
            ? []
            : message.Photo
                .GroupBy(p => p.FileUniqueId)
                .Select(g => g.OrderByDescending(p => p.FileSize ?? 0).First().FileId)
                .ToList();

        return new ParsedTelegramProduct(
            Name: name.Trim(),
            PriceUsd: price,
            Category: category.Trim(),
            Brand: (brand ?? "Sin marca").Trim(),
            Description: (description ?? string.Empty).Trim(),
            PhotoFileIds: photoFileIds);
    }

    private static string? ExtractField(string text, string emoji)
    {
        string pattern = $@"{emoji}\s*(.+?)(?:\n|$)";
        Match match = Regex.Match(text, pattern);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    private static string? ExtractDescription(string text, string emoji)
    {
        string pattern = $@"{emoji}\s*(.+?)(?=#importar|$)";
        Match match = Regex.Match(text, pattern, RegexOptions.Singleline);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }
}
