using MediatR;
using Microsoft.Extensions.Configuration;
using SelectStoreAR.Application.DTOs;
using SelectStoreAR.Application.Interfaces;
using SelectStoreAR.Domain.Entities;
using SelectStoreAR.Domain.Interfaces;

namespace SelectStoreAR.Application.Commands.Auth;

public sealed class OAuthLoginHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IJwtService jwtService,
    IConfiguration configuration)
    : IRequestHandler<OAuthLoginCommand, OAuthLoginResult>
{
    public async Task<OAuthLoginResult> Handle(OAuthLoginCommand request, CancellationToken cancellationToken)
    {
        User? user = await userRepository
            .GetByExternalLoginAsync(request.Provider, request.ProviderKey, cancellationToken)
            .ConfigureAwait(false);

        bool isNew = false;

        if (user is null)
        {
            // Nuevo usuario — determinar rol
            string adminEmail = configuration["Auth:AdminEmail"] ?? string.Empty;
            string role = request.Email.Equals(adminEmail, StringComparison.OrdinalIgnoreCase)
                ? "admin"
                : "user";

            // Buscar si existe por email (login con distinto provider)
            user = await userRepository.GetByEmailAsync(request.Email, cancellationToken).ConfigureAwait(false);

            if (user is null)
            {
                user = User.Create(request.Email, request.Name, role, request.PictureUrl);
                userRepository.Add(user);
                isNew = true;
            }

            user.UpsertExternalLogin(request.Provider, request.ProviderKey, request.Email, request.Name);
            user.UpdatePicture(request.PictureUrl);
        }
        else
        {
            user.UpsertExternalLogin(request.Provider, request.ProviderKey, request.Email, request.Name);
            user.UpdatePicture(request.PictureUrl);
        }

        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        string token = jwtService.GenerateToken(user);

        UserDto userDto = new(user.Id, user.Email, user.Name, user.Role, user.PictureUrl, user.Phone);
        return new OAuthLoginResult(userDto, token, isNew);
    }
}
