namespace SelectStoreAR.Application.DTOs;

public sealed record UserDto(
    Guid Id,
    string Email,
    string Name,
    string Role,
    string? PictureUrl,
    string? Phone);

public sealed record OAuthCallbackCommand(
    string Provider,
    string ProviderKey,
    string Email,
    string Name,
    string? PictureUrl);
