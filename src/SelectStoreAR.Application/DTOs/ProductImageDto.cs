namespace SelectStoreAR.Application.DTOs;

public sealed record ProductImageDto(
    Guid Id,
    string Url,
    string ThumbnailUrl,
    string MediumUrl,
    string? AltText,
    int SortOrder);
