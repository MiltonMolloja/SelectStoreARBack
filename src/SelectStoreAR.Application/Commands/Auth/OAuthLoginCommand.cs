using MediatR;
using SelectStoreAR.Application.DTOs;

namespace SelectStoreAR.Application.Commands.Auth;

public sealed record OAuthLoginCommand(
    string Provider,
    string ProviderKey,
    string Email,
    string Name,
    string? PictureUrl) : IRequest<OAuthLoginResult>;

public sealed record OAuthLoginResult(UserDto User, string Token, bool IsNew);
