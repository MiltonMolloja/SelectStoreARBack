using MediatR;
using SelectStoreAR.Application.DTOs;

namespace SelectStoreAR.Application.Queries.Auth;

public sealed record GetCurrentUserQuery(Guid UserId) : IRequest<UserDto?>;
