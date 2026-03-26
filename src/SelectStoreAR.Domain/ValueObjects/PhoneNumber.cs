using System.Text.RegularExpressions;
using SelectStoreAR.Domain.Common;

namespace SelectStoreAR.Domain.ValueObjects;

public sealed record PhoneNumber
{
    private PhoneNumber(string value) => Value = value;

    public string Value { get; }

    public static PhoneNumber Create(string phone)
    {
        string cleaned = Regex.Replace(phone, @"[^\d+]", string.Empty);

        // Argentina: +54 + 10-11 digits (mobile: +549xxxxxxxx = 13 total, landline: +54xxxxxxxx = 12 total)
        if (!Regex.IsMatch(cleaned, @"^\+?54\d{10,11}$") &&
            !Regex.IsMatch(cleaned, @"^\d{10,11}$"))
        {
            throw new DomainException($"Invalid phone number: {phone}");
        }

        if (!cleaned.StartsWith('+'))
        {
            cleaned = "+54" + cleaned;
        }

        return new PhoneNumber(cleaned);
    }

    public string FormatForWhatsApp() => Value.Replace("+", string.Empty, StringComparison.Ordinal);

    public override string ToString() => Value;
}
