namespace SelectStoreAR.Application.DTOs;

public sealed record CategoryDto(
    Guid Id,
    string Name,
    string Slug,
    Guid? ParentId,
    decimal? DefaultMarkup,
    int SortOrder,
    string? ImageUrl,
    int ProductCount,
    IReadOnlyList<CategoryDto> Children);
