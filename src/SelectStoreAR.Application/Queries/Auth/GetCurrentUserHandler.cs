using MediatR;
using SelectStoreAR.Application.DTOs;
using SelectStoreAR.Domain.Entities;
using SelectStoreAR.Domain.Interfaces;

namespace SelectStoreAR.Application.Queries.Auth;

public sealed class GetCurrentUserHandler(IUserRepository userRepository)
    : IRequestHandler<GetCurrentUserQuery, UserDto?>
{
    public async Task<UserDto?> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        User? user = await userRepository.GetByIdAsync(request.UserId, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return null;
        }

        return new UserDto(user.Id, user.Email, user.Name, user.Role, user.PictureUrl, user.Phone);
    }
}
