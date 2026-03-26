using SelectStoreAR.Domain.Common;

namespace SelectStoreAR.Domain.ValueObjects;

public sealed record Markup
{
    public decimal Percentage { get; }

    private Markup(decimal percentage) => Percentage = percentage;

    public static Markup Create(decimal percentage)
    {
        if (percentage < 0)
        {
            throw new DomainException("Markup cannot be negative");
        }

        if (percentage > 500)
        {
            throw new DomainException("Markup cannot exceed 500%");
        }

        return new Markup(percentage);
    }

    public override string ToString() => $"{Percentage}%";
}
