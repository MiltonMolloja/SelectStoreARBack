using System.Text.RegularExpressions;

namespace SelectStoreAR.Application.Services;

/// <summary>
/// Parsea listas de precios del canal Telegram "NetShop ARG".
///
/// Formato real observado:
///   ARMAF                              ← encabezado de marca
///   **Club de Nuit Sillage** 105ml u$36✅
///   **Odyssey Mega** 100ml u$37✅
///   **Odyssey Montagne** 100ml u$55✅🆕
///   **His Confession** 100ml u$         ← sin precio → ignorar
///
///   PIXEL IMPORTADO
///   9A 128 u$490🏭
///   10 128 US u$680🏭
///
/// Indicadores de disponibilidad:
///   ✅   = disponible / en stock
///   🏭   = en depósito
///   🛬   = llegando
///   📭   = a pedido
///   (sin precio) = sin stock / consultar → ignorar
/// </summary>
public static class TelegramPriceListParser
{
    public sealed record PriceListItem(
        string Name,
        string Brand,
        string Category,
        decimal PriceUsd,
        string? SizeOrVariant,
        string? Inspiration,        // ej: "Dior-Sauvage Elixir"
        string AvailabilityStatus,  // "available" | "warehouse" | "arriving" | "on_demand"
        string RawLine);

    public sealed record PriceListResult(
        string? DetectedBrand,
        IReadOnlyList<PriceListItem> Items,
        int ParsedCount,
        int SkippedCount);

    // Precio con número obligatorio: u$36, u$36.5, us$490, usd 490
    private static readonly Regex PriceWithNumberRegex = new(
        @"u\s*s?\s*\$\s*(\d[\d.,]*)(?:\s*a\s*(\d[\d.,]*))?|usd\s+(\d[\d.,]*)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Tamaño: 100ml, 60ml, 1TB, 256GB, etc.
    private static readonly Regex SizeRegex = new(
        @"\b(\d+\s*(?:ml|gb|tb|mb|g))\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Descripción entre paréntesis: (Dior-Sauvage), (YSL-Y)
    private static readonly Regex InspirationRegex = new(
        @"\(([^)]{3,80})\)",
        RegexOptions.Compiled);

    // Emojis y marcadores a eliminar del nombre
    private static readonly Regex CleanNameRegex = new(
        @"[✅🏭🛬📭🆕🎁📸📷🎥🎬🗣🚫👱‍♀️🧔‍♂️🐶🐾🌹🍰🥭👠🐍🦅🐝🦌🐎👸🏻👸🏼👨🏼‍🦱]|" +
        @"\*{1,2}|_{1,2}|~{1,2}|`{1,3}",
        RegexOptions.Compiled);

    // Prefijos de sublista: "_ ", "- ", "• "
    private static readonly Regex SublistPrefixRegex = new(
        @"^[-_•]\s*",
        RegexOptions.Compiled);

    public static PriceListResult Parse(string text)
    {
        // Limpiar HTML del export de Telegram (si viene del HTML)
        string cleaned = CleanHtml(text);

        List<PriceListItem> items = [];
        int skipped = 0;

        string[] lines = cleaned.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        string currentBrand = string.Empty;
        string currentCategory = string.Empty;

        foreach (string rawLine in lines)
        {
            string line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.Length < 2)
            {
                continue;
            }

            // Saltar líneas informativas (notas, avisos, etc.)
            if (IsInformationalLine(line))
            {
                skipped++;
                continue;
            }

            // Verificar si tiene precio con número
            Match priceMatch = PriceWithNumberRegex.Match(line);

            if (!priceMatch.Success)
            {
                // Sin precio → puede ser encabezado de sección
                string strippedLine = CleanMarkdown(line);

                if (IsLikelySectionHeader(strippedLine))
                {
                    (currentBrand, currentCategory) = ParseSectionHeader(strippedLine);
                }

                skipped++;
                continue;
            }

            // Tiene precio con número → parsear como producto
            decimal price = ExtractPrice(priceMatch);
            if (price <= 0)
            {
                skipped++;
                continue;
            }

            string namePart = ExtractNamePart(line, priceMatch.Index);
            if (string.IsNullOrWhiteSpace(namePart))
            {
                skipped++;
                continue;
            }

            string? size = ExtractSize(namePart);
            string? inspiration = ExtractInspiration(line);
            string availability = DetectAvailability(line);
            string cleanName = BuildProductName(currentBrand, namePart, size);

            items.Add(new PriceListItem(
                Name: cleanName,
                Brand: currentBrand,
                Category: currentCategory,
                PriceUsd: price,
                SizeOrVariant: size,
                Inspiration: inspiration,
                AvailabilityStatus: availability,
                RawLine: rawLine));
        }

        string? detectedBrand = items.Count > 0 ? items[0].Brand : null;
        return new PriceListResult(detectedBrand, items, items.Count, skipped);
    }

    private static string CleanHtml(string text)
    {
        // Convertir <br> a salto de línea
        text = Regex.Replace(text, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
        // Preservar texto de <strong> y <em> sin las etiquetas
        text = Regex.Replace(text, @"</?(?:strong|em|b|i|u|s)>", string.Empty, RegexOptions.IgnoreCase);
        // Eliminar otras etiquetas HTML
        text = Regex.Replace(text, @"<[^>]+>", string.Empty);
        // Decodificar entidades HTML básicas
        text = text.Replace("&amp;", "&", StringComparison.Ordinal)
                   .Replace("&lt;", "<", StringComparison.Ordinal)
                   .Replace("&gt;", ">", StringComparison.Ordinal)
                   .Replace("&apos;", "'", StringComparison.Ordinal)
                   .Replace("&quot;", "\"", StringComparison.Ordinal)
                   .Replace("&laquo;", "«", StringComparison.Ordinal)
                   .Replace("&raquo;", "»", StringComparison.Ordinal);
        return text;
    }

    private static string CleanMarkdown(string text)
    {
        return CleanNameRegex.Replace(text, string.Empty).Trim();
    }

    private static bool IsInformationalLine(string line)
    {
        string lower = line.ToLowerInvariant();
        return lower.StartsWith("✅", StringComparison.Ordinal)
            || lower.StartsWith("ℹ", StringComparison.Ordinal)
            || lower.Contains("no se hacen")
            || lower.Contains("garantia")
            || lower.Contains("garantía")
            || lower.Contains("pedidos no tienen")
            || lower.Contains("no se escucha")
            || lower.Contains("pagos:")
            || lower.Contains("google maps")
            || lower.Contains("http")
            || (lower.StartsWith("(", StringComparison.Ordinal) && !line.Contains("u$"))
            || lower.Contains("vez número")
            || lower.Contains("retíralo ya")
            || lower.Contains("llegando")
            || lower.Contains("deposito")
            || lower.Contains("a pedido");
    }

    private static bool IsLikelySectionHeader(string line)
    {
        if (line.Length < 2 || line.Length > 60)
        {
            return false;
        }

        // Si tiene paréntesis con precio de referencia → no es header
        if (line.Contains("u$", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Mayúsculas predominantes → header
        int upperCount = line.Count(char.IsUpper);
        int letterCount = line.Count(char.IsLetter);

        return letterCount > 0 && (double)upperCount / letterCount > 0.5;
    }

    private static (string brand, string category) ParseSectionHeader(string header)
    {
        string upper = header.ToUpperInvariant().Trim();

        // Detectar categoría por palabras clave
        string category = upper switch
        {
            _ when ContainsAny(upper, "IPHONE", "APPLE", "IPAD", "MACBOOK", "AIRPODS") => "Celulares",
            _ when ContainsAny(upper, "SAMSUNG", "GALAXY") => "Celulares",
            _ when ContainsAny(upper, "XIAOMI", "REDMI", "POCO") => "Celulares",
            _ when ContainsAny(upper, "PIXEL", "GOOGLE") => "Celulares",
            _ when ContainsAny(upper, "PLAYSTATION", "PS5", "PS4", "XBOX", "NINTENDO", "SWITCH") => "Consolas",
            _ when IsPerfumeBrand(upper) => "Perfumes",
            _ when ContainsAny(upper, "CANON", "SONY", "NIKON", "GOPRO", "FUJIFILM", "INSTA360") => "Camaras",
            _ when ContainsAny(upper, "LAPTOP", "MACBOOK", "NOTEBOOK", "LENOVO", "HP", "DELL") => "Laptops",
            _ when ContainsAny(upper, "TABLET", "IPAD") => "Tablets",
            _ => "Tecnologia",
        };

        // Limpiar el nombre de la marca (capitalizar)
        string brand = ToTitleCase(header.Trim());

        return (brand, category);
    }

    private static decimal ExtractPrice(Match match)
    {
        // Prioridad: grupo 3 (usd X) > grupo 1 (u$X) > mínimo del rango
        string rawPrice = match.Groups[3].Success ? match.Groups[3].Value : match.Groups[1].Value;

        rawPrice = rawPrice.Replace(".", string.Empty, StringComparison.Ordinal)
                           .Replace(",", ".", StringComparison.Ordinal)
                           .Trim();

        return decimal.TryParse(rawPrice, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out decimal price) ? price : 0;
    }

    private static string ExtractNamePart(string line, int priceIndex)
    {
        string before = line[..priceIndex].Trim();

        // Eliminar prefijos de sublista (_ - •)
        before = SublistPrefixRegex.Replace(before, string.Empty);

        // Eliminar indicadores de disponibilidad del nombre
        before = Regex.Replace(before, @"[✅🏭🛬📭🆕🎁]", string.Empty).Trim();

        // Eliminar markdown
        before = CleanMarkdown(before);

        // Eliminar inspiraciones entre paréntesis del nombre
        before = InspirationRegex.Replace(before, string.Empty).Trim();

        // Limpiar múltiples espacios
        return Regex.Replace(before, @"\s{2,}", " ").Trim();
    }

    private static string? ExtractSize(string namePart)
    {
        Match sizeMatch = SizeRegex.Match(namePart);
        if (!sizeMatch.Success)
        {
            return null;
        }

        string size = sizeMatch.Value.Trim();
        return Regex.Replace(size, @"(\d+)\s*(ml|gb|tb|mb|g)", m =>
            m.Groups[1].Value + m.Groups[2].Value.ToUpperInvariant(), RegexOptions.IgnoreCase);
    }

    private static string? ExtractInspiration(string line)
    {
        Match match = InspirationRegex.Match(line);
        if (!match.Success)
        {
            return null;
        }

        string inspiration = match.Groups[1].Value.Trim();
        // Solo retornar si parece una referencia a perfume/producto (contiene guión)
        return inspiration.Contains('-', StringComparison.Ordinal) ? inspiration : null;
    }

    private static string DetectAvailability(string line)
    {
        if (line.Contains("✅", StringComparison.Ordinal))
        {
            return "available";
        }

        if (line.Contains("🏭", StringComparison.Ordinal))
        {
            return "warehouse";
        }

        if (line.Contains("🛬", StringComparison.Ordinal))
        {
            return "arriving";
        }

        if (line.Contains("📭", StringComparison.Ordinal))
        {
            return "on_demand";
        }

        return "available"; // Si tiene precio pero sin emoji, asumir disponible
    }

    private static string BuildProductName(string brand, string namePart, string? size)
    {
        // Remover el tamaño del nombre para evitar duplicarlo
        string nameWithoutSize = size is not null
            ? Regex.Replace(namePart, Regex.Escape(size), string.Empty, RegexOptions.IgnoreCase).Trim()
            : namePart;

        nameWithoutSize = Regex.Replace(nameWithoutSize, @"\s{2,}", " ").Trim();
        nameWithoutSize = nameWithoutSize.Trim([',', '-', '.']);

        // No agregar la marca si ya está en el nombre
        if (!string.IsNullOrEmpty(brand) &&
            !nameWithoutSize.Contains(brand, StringComparison.OrdinalIgnoreCase))
        {
            return $"{brand} {nameWithoutSize}".Trim();
        }

        return nameWithoutSize;
    }

    private static string ToTitleCase(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        return System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(text.ToLowerInvariant());
    }

    private static bool IsPerfumeBrand(string upper)
    {
        string[] brands =
        [
            "PERFUME", "EDP", "EDT", "LATTAFA", "ARMAF", "AFNAN",
            "RASASI", "MAISON ALHAMBRA", "AL HARAMAIN", "LANCOME", "ARMANI",
            "PACO RABANNE", "XERJOFF", "MONT BLANC", "ANFAR", "BHARARA", "DUMONT",
            "ASDAAF", "AL WATANIAH", "RAYHAAN", "FRENCH AVENUE",
        ];
        return brands.Any(b => upper.Contains(b, StringComparison.OrdinalIgnoreCase));
    }

    private static bool ContainsAny(string source, params string[] values)
    {
        return values.Any(v => source.Contains(v, StringComparison.OrdinalIgnoreCase));
    }
}
