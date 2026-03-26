using System.Text.RegularExpressions;
using SelectStoreAR.Domain.Common;

namespace SelectStoreAR.Domain.ValueObjects;

public sealed record Slug
{
    private Slug(string value) => Value = value;

    public string Value { get; }

    public static Slug Create(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new DomainException("Slug source text cannot be empty");
        }

        // ToLowerInvariant is intentional: slugs are always lowercase
        string slug = text
            .ToLowerInvariant()
            .Trim()
            .Replace("á", "a", StringComparison.Ordinal)
            .Replace("é", "e", StringComparison.Ordinal)
            .Replace("í", "i", StringComparison.Ordinal)
            .Replace("ó", "o", StringComparison.Ordinal)
            .Replace("ú", "u", StringComparison.Ordinal)
            .Replace("ñ", "n", StringComparison.Ordinal)
            .Replace("ü", "u", StringComparison.Ordinal)
            .Replace("ª", "a", StringComparison.Ordinal)
            .Replace("º", "o", StringComparison.Ordinal);

        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", string.Empty);
        slug = Regex.Replace(slug, @"\s+", "-");
        slug = Regex.Replace(slug, @"-+", "-");
        slug = slug.Trim('-');

        if (string.IsNullOrEmpty(slug))
        {
            throw new DomainException("Generated slug is empty");
        }

        return new Slug(slug);
    }

    public override string ToString() => Value;

    public static implicit operator string(Slug slug) => slug.Value;
}
