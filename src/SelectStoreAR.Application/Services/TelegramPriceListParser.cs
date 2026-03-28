using System.Text.RegularExpressions;

namespace SelectStoreAR.Application.Services;

/// <summary>
/// Parsea listas de precios del canal Telegram "NetShop ARG".
///
/// Formatos soportados:
///   ARMAF                              ← encabezado de marca (mayúsculas)
///   _Sillage 105ml u$36✅              ← sublista con prefijo _
///   Club de Nuit:                      ← sub-sección (no cambia marca)
///   💻Lenovo Gaming I5 u$655🏭         ← emoji pegado al nombre
///   PARLANTE PORTATIL                  ← header sin precio
///   🔊G200 Speaker 5W                  ← descripción en línea propia
///   u$9✅                              ← precio en línea separada
///   (Creed-Aventus)                    ← inspiración en línea propia → ignorar
///   - Ceramic Pink/Rose Gold           ← variante → ignorar
///   u$ / u$                            ← sin precio → ignorar
/// </summary>
public static class TelegramPriceListParser
{
    public sealed record PriceListItem(
        string Name,
        string Brand,
        string Category,
        decimal PriceUsd,
        string? SizeOrVariant,
        string? Inspiration,
        string AvailabilityStatus,
        string RawLine);

    public sealed record PriceListResult(
        string? DetectedBrand,
        IReadOnlyList<PriceListItem> Items,
        int ParsedCount,
        int SkippedCount);

    // Precio USD con número obligatorio: u$36, u$36.5, us$490, usd 490, 345usdt, 355us
    // El punto final (u$1210.) se excluye del precio con lookahead
    private static readonly Regex PriceUsdRegex = new(
        @"u\s*s?\s*\$\s*(\d[\d.,]*)(?:\s*a\s*(\d[\d.,]*))?|usd\s+(\d[\d.,]*)|(\d[\d.,]*)\s*us(?:dt?)?(?:\b|$)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Precio en pesos: $745, *$500 — los ignoramos (no son USD)
    private static readonly Regex PricePesosOnlyRegex = new(
        @"(?<!\w)[*]?\$\s*\d[\d.,]*",
        RegexOptions.Compiled);

    // Tamaño: 100ml, 60ml, 1TB, 256GB, etc.
    private static readonly Regex SizeRegex = new(
        @"\b(\d+\s*(?:ml|gb|tb|mb|g))\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Inspiración entre paréntesis: (Dior-Sauvage), (YSL-Y)
    private static readonly Regex InspirationRegex = new(
        @"\(([^)]{3,80})\)",
        RegexOptions.Compiled);

    // Replacement char y emojis a eliminar del nombre final
    private static readonly Regex CleanNameRegex = new(
        @"[\uFFFD]|" +
        @"[✅🏭🛬📭🆕🎁📸📷🎥🎬🗣🚫🔊🎤📽💻🥌🐶🐾🌹🍰🥭👠🐍🦅🐝🦌🐎]|" +
        @"[\uD83C-\uDBFF][\uDC00-\uDFFF]|" +
        @"[\u2600-\u27BF]|" +
        @"[\uD83D][\uDC00-\uDFFF]|" +
        @"\*{1,2}|_{1,2}|~{1,2}|`{1,3}",
        RegexOptions.Compiled);

    // Prefijos de sublista: "_ ", "- ", "• "
    private static readonly Regex SublistPrefixRegex = new(
        @"^[-_•]\s*",
        RegexOptions.Compiled);

    // Línea de variante: comienza con "- " y NO tiene precio USD
    private static readonly Regex VariantLineRegex = new(
        @"^-\s+\S",
        RegexOptions.Compiled);

    public static PriceListResult Parse(string text)
    {
        string cleaned = CleanHtml(text);

        List<PriceListItem> items = [];
        int skipped = 0;

        string[] lines = cleaned.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        string currentBrand = string.Empty;
        string currentCategory = string.Empty;
        string lastDescriptionLine = string.Empty;

        foreach (string rawLine in lines)
        {
            string line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.Length < 2)
            {
                continue;
            }

            // Variante de producto anterior (- Ceramic Pink) → ignorar
            if (IsVariantLine(line))
            {
                skipped++;
                continue;
            }

            // Línea informativa → ignorar
            if (IsInformationalLine(line))
            {
                skipped++;
                continue;
            }

            // ¿Tiene precio USD con número?
            Match priceMatch = PriceUsdRegex.Match(line);

            if (!priceMatch.Success)
            {
                // ¿Tiene solo precio en pesos? → ignorar sin alterar lastDescriptionLine
                if (PricePesosOnlyRegex.IsMatch(line) && !PriceUsdRegex.IsMatch(line))
                {
                    skipped++;
                    continue;
                }

                string strippedLine = CleanMarkdown(line);

                if (IsLikelySectionHeader(strippedLine))
                {
                    (currentBrand, currentCategory) = ParseSectionHeader(strippedLine);
                    lastDescriptionLine = string.Empty;
                }
                else
                {
                    // Guardar como posible nombre si la siguiente línea es solo precio
                    lastDescriptionLine = strippedLine;
                }

                skipped++;
                continue;
            }

            decimal price = ExtractPrice(priceMatch);
            if (price <= 0)
            {
                // u$ sin número (sin stock) → no actualizar lastDescriptionLine
                skipped++;
                continue;
            }

            string namePart = ExtractNamePart(line, priceMatch.Index);

            // Precio en línea sola → usar descripción de línea anterior
            if (string.IsNullOrWhiteSpace(namePart) && !string.IsNullOrWhiteSpace(lastDescriptionLine))
            {
                namePart = lastDescriptionLine;
            }

            lastDescriptionLine = string.Empty;

            if (string.IsNullOrWhiteSpace(namePart))
            {
                skipped++;
                continue;
            }

            string? size = ExtractSize(namePart);
            string? inspiration = ExtractInspiration(line);
            string availability = DetectAvailability(line);
            string cleanName = BuildProductName(currentBrand, namePart, size);

            if (string.IsNullOrWhiteSpace(cleanName))
            {
                skipped++;
                continue;
            }

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
        text = Regex.Replace(text, @"(?<![\uD800-\uDBFF])[\uDC00-\uDFFF]|[\uD800-\uDBFF](?![\uDC00-\uDFFF])", string.Empty);
        text = Regex.Replace(text, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"</?(?:strong|em|b|i|u|s)>", string.Empty, RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"<[^>]+>", string.Empty);
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

    private static bool IsVariantLine(string line)
    {
        // "- Ceramic Pink/Rose Gold" → variante, no producto
        // Pero "- Aoud edition 60ml u$25✅" SÍ es producto (tiene precio)
        return VariantLineRegex.IsMatch(line) && !PriceUsdRegex.IsMatch(line);
    }

    private static bool IsInformationalLine(string line)
    {
        // Si tiene precio USD nunca es informacional
        if (PriceUsdRegex.IsMatch(line))
        {
            return false;
        }

        string lower = line.ToLowerInvariant();
        return lower.StartsWith("ℹ", StringComparison.Ordinal)
            || lower.Contains("no se hacen")
            || lower.Contains("garantia")
            || lower.Contains("garantía")
            || lower.Contains("s/g sin")
            || lower.Contains("pedidos no tienen")
            || lower.Contains("no se escucha")
            || lower.Contains("pagos:")
            || lower.Contains("google maps")
            || lower.Contains("http")
            || lower.Contains("silicone case")
            || lower.Contains("el stock es bajo")
            || (lower.StartsWith("(", StringComparison.Ordinal) && !line.Contains("u$", StringComparison.OrdinalIgnoreCase))
            || lower.Contains("vez número")
            || lower.Contains("retíralo ya")
            || lower.Contains("llegando")
            || lower.Contains("deposito")
            || lower.Contains("a pedido")
            || lower.Contains("no son la")
            || lower.Contains("incluye ")
            || lower.Contains("preactivado")
            || lower.Contains("sin garantía")
            || lower == "lte"
            || lower == "5g"
            || lower == "4g";
    }

    private static readonly string[] KnownBrandKeywords =
    [
        // Celulares
        "IPHONE", "APPLE", "IPAD", "MACBOOK", "AIRPODS",
        "SAMSUNG", "GALAXY",
        "XIAOMI", "REDMI", "POCO",
        "PIXEL", "GOOGLE",
        "MOTOROLA", "MOTO",
        "REALME", "INFINIX", "TECNO", "OPPO", "VIVO",
        "ONEPLUS", "HONOR", "HUAWEI",
        // Consolas / VR / Gaming
        "PLAYSTATION", "PS5", "PS4", "XBOX", "NINTENDO", "SWITCH",
        "META QUEST", "QUEST", "VR",
        "DUALSENSE", "JOYSTICK",
        // Cámaras / Drones
        "CANON", "SONY", "NIKON", "GOPRO", "FUJIFILM", "INSTA360",
        "DJI", "DRONE", "OSMO",
        // Laptops / Monitores / TVs
        "LAPTOP", "NOTEBOOK", "LENOVO", "HP", "DELL", "ACER", "ASUS",
        "MONITOR", "ALIENWARE",
        "SAMSUNG TV", "SMART TV", "QLED", "NOBLEX", "TCL", "LG", "PHILIPS",
        // Audio / Accesorios
        "JBL", "PARTYBOX", "BOSE", "BEATS", "SONY WH",
        "PARLANTE", "SPEAKER", "HEADPHONE", "EARBUDS",
        "GARMIN", "SMARTWATCH", "FORERUNNER", "FENIX",
        "RAYBAN", "RAY-BAN", "RAY BAN",
        // Tablets / Smart Home
        "TABLET", "AMAZON", "ECHO", "KINDLE", "ALEXA", "CHROMECAST",
        // Electrodomésticos
        "DYSON", "ASPIRADORA",
        "PROYECTOR", "FREESTYLE",
        // Varios
        "CLASIFICADORA", "CARGADOR PORTATIL", "POWER BANK",
        // Perfumes
        "PERFUME", "EDP", "EDT",
        "LATTAFA", "ARMAF", "AFNAN",
        "RASASI", "MAISON ALHAMBRA", "AL HARAMAIN", "LANCOME", "ARMANI",
        "PACO RABANNE", "XERJOFF", "MONT BLANC", "ANFAR", "BHARARA",
        "DUMONT", "ASDAAF", "AL WATANIAH", "RAYHAAN", "FRENCH AVENUE",
        "EMPORIO",
    ];

    // Sub-secciones que NO deben cambiar el brand actual
    private static readonly HashSet<string> KnownSubSections = new(StringComparer.OrdinalIgnoreCase)
    {
        "LTE", "5G", "4G", "5G LTE",
        "IPHONE NUEVOS", "IPHONE CPO", "IPHONE USADOS",
        "MAC MINI", "MAC STUDIO", "IMAC", "IMAC M4", "IMAC M3",
        "WATCH", "AIRPODS", "IPAD",
        "NUEVOS", "CPO", "REACONDICIONADOS",
        "WATCH SAMSUNG", "GALAXY WATCH",
    };

    private static bool IsLikelySectionHeader(string line)
    {
        if (line.Length < 2 || line.Length > 80)
        {
            return false;
        }

        if (PriceUsdRegex.IsMatch(line))
        {
            return false;
        }

        // Termina con ":" → sub-sección → NO cambiar brand
        if (line.TrimEnd().EndsWith(":", StringComparison.Ordinal))
        {
            return false;
        }

        // Sub-sección conocida → NO cambiar brand
        string trimmed = line.Trim().TrimEnd(':');
        if (KnownSubSections.Contains(trimmed))
        {
            return false;
        }

        string upper = line.ToUpperInvariant();

        if (KnownBrandKeywords.Any(k => upper.Contains(k, StringComparison.Ordinal)))
        {
            return true;
        }

        int upperCount = line.Count(char.IsUpper);
        int letterCount = line.Count(char.IsLetter);

        return letterCount > 0 && (double)upperCount / letterCount > 0.5;
    }

    private static (string brand, string category) ParseSectionHeader(string header)
    {
        string upper = header.ToUpperInvariant().Trim();

        string category = upper switch
        {
            _ when ContainsAny(upper, "IPHONE", "APPLE", "AIRPODS") => "Celulares",
            _ when ContainsAny(upper, "SAMSUNG", "GALAXY") && !ContainsAny(upper, "MONITOR", "TV", "SMART TV") => "Celulares",
            _ when ContainsAny(upper, "XIAOMI", "REDMI", "POCO") && !ContainsAny(upper, "ASPIRADORA", "MIJIA") => "Celulares",
            _ when ContainsAny(upper, "PIXEL", "GOOGLE") && !ContainsAny(upper, "TV", "CHROMECAST") => "Celulares",
            _ when ContainsAny(upper, "MOTOROLA", "REALME", "INFINIX", "TECNO", "OPPO", "VIVO", "ONEPLUS", "HONOR", "HUAWEI") => "Celulares",
            _ when ContainsAny(upper, "PLAYSTATION", "PS5", "PS4", "XBOX", "DUALSENSE") => "Consolas",
            _ when ContainsAny(upper, "NINTENDO", "SWITCH") => "Consolas",
            _ when ContainsAny(upper, "META QUEST", "QUEST", "VR2") => "Consolas",
            _ when IsPerfumeBrand(upper) => "Perfumes",
            _ when ContainsAny(upper, "CANON", "NIKON", "GOPRO", "FUJIFILM", "INSTA360") => "Camaras",
            _ when ContainsAny(upper, "DJI", "DRONE", "OSMO") => "Camaras",
            _ when ContainsAny(upper, "MACBOOK", "LAPTOP", "NOTEBOOK", "LENOVO", "HP", "DELL", "ACER", "ASUS") => "Laptops",
            _ when ContainsAny(upper, "MONITOR", "ALIENWARE") => "Monitores",
            _ when ContainsAny(upper, "SMART TV", "QLED", "OLED TV", "NOBLEX", "TCL", "PHILIPS") => "TVs",
            _ when ContainsAny(upper, "LG") && ContainsAny(upper, "TV", "NANO") => "TVs",
            _ when ContainsAny(upper, "TABLET", "IPAD") => "Tablets",
            _ when ContainsAny(upper, "KINDLE") => "Tablets",
            _ when ContainsAny(upper, "AMAZON", "ECHO", "ALEXA", "CHROMECAST") => "SmartHome",
            _ when ContainsAny(upper, "GARMIN", "SMARTWATCH", "FORERUNNER", "FENIX", "INSTINCT") => "Smartwatches",
            _ when ContainsAny(upper, "JBL", "BOSE", "BEATS", "PARTYBOX", "PARLANTE", "SPEAKER") => "Audio",
            _ when ContainsAny(upper, "PROYECTOR", "FREESTYLE") => "Audio",
            _ when ContainsAny(upper, "DYSON") => "Electrodomesticos",
            _ when ContainsAny(upper, "ASPIRADORA", "MIJIA") => "Electrodomesticos",
            _ when ContainsAny(upper, "RAYBAN", "RAY-BAN", "RAY BAN") => "Accesorios",
            _ when ContainsAny(upper, "VARIOS", "CLASIFICADORA", "CARGADOR", "POWER BANK") => "Varios",
            _ => "Tecnologia",
        };

        // Limpiar emojis y replacement chars del brand
        string brand = CleanMarkdown(ToTitleCase(header.Trim()));
        brand = Regex.Replace(brand, @"\s{2,}", " ").Trim().TrimEnd(':');

        return (brand, category);
    }

    private static decimal ExtractPrice(Match match)
    {
        string rawPrice = match.Groups[3].Success ? match.Groups[3].Value
                        : match.Groups[4].Success ? match.Groups[4].Value
                        : match.Groups[1].Value;

        rawPrice = rawPrice.TrimEnd('.')
                           .Replace(".", string.Empty, StringComparison.Ordinal)
                           .Replace(",", ".", StringComparison.Ordinal)
                           .Trim();

        return decimal.TryParse(rawPrice, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out decimal price) ? price : 0;
    }

    private static string ExtractNamePart(string line, int priceIndex)
    {
        string before = line[..priceIndex].Trim();

        before = SublistPrefixRegex.Replace(before, string.Empty);
        before = Regex.Replace(before, @"[✅🏭🛬📭🆕🎁]", string.Empty).Trim();
        before = CleanMarkdown(before);
        before = InspirationRegex.Replace(before, string.Empty).Trim();
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

        return "available";
    }

    private static string BuildProductName(string brand, string namePart, string? size)
    {
        string nameWithoutSize = size is not null
            ? Regex.Replace(namePart, Regex.Escape(size), string.Empty, RegexOptions.IgnoreCase).Trim()
            : namePart;

        nameWithoutSize = Regex.Replace(nameWithoutSize, @"\s{2,}", " ").Trim();
        nameWithoutSize = nameWithoutSize.Trim([',', '-', '.', ':', ' ']);

        if (string.IsNullOrWhiteSpace(nameWithoutSize))
        {
            return string.Empty;
        }

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
            "PERFUME", "EDP", "EDT",
            "LATTAFA", "ARMAF", "AFNAN", "RASASI",
            "MAISON ALHAMBRA", "AL HARAMAIN", "LANCOME", "LANCÔME",
            "ARMANI", "EMPORIO ARMANI",
            "PACO RABANNE", "XERJOFF", "MONT BLANC", "ANFAR", "BHARARA",
            "DUMONT", "ASDAAF", "AL WATANIAH", "RAYHAAN", "FRENCH AVENUE",
        ];
        return brands.Any(b => upper.Contains(b, StringComparison.OrdinalIgnoreCase));
    }

    private static bool ContainsAny(string source, params string[] values)
    {
        return values.Any(v => source.Contains(v, StringComparison.OrdinalIgnoreCase));
    }
}
