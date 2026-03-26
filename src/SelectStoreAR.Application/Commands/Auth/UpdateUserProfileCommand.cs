using FluentValidation;
using MediatR;
using SelectStoreAR.Application.DTOs;
using SelectStoreAR.Domain.Common;
using SelectStoreAR.Domain.Entities;
using SelectStoreAR.Domain.Interfaces;

namespace SelectStoreAR.Application.Commands.Auth;

public sealed record UpdateUserProfileCommand(
    Guid UserId,
    string Name,
    string? Phone) : IRequest<UserDto>;

public sealed class UpdateUserProfileHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateUserProfileCommand, UserDto>
{
    public async Task<UserDto> Handle(UpdateUserProfileCommand request, CancellationToken cancellationToken)
    {
        User user = await userRepository.GetByIdAsync(request.UserId, cancellationToken).ConfigureAwait(false)
            ?? throw new DomainException($"Usuario '{request.UserId}' no encontrado");

        user.UpdateProfile(request.Name, request.Phone);
        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new UserDto(user.Id, user.Email, user.Name, user.Role, user.PictureUrl, user.Phone);
    }
}

public sealed class UpdateUserProfileValidator : AbstractValidator<UpdateUserProfileCommand>
{
    public UpdateUserProfileValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es requerido")
            .MaximumLength(200).WithMessage("El nombre no puede superar 200 caracteres");

        RuleFor(x => x.Phone)
            .Matches(@"^\+?[\d\s\-\(\)]{8,20}$").WithMessage("El formato del teléfono no es válido")
            .When(x => !string.IsNullOrEmpty(x.Phone));
    }
}
