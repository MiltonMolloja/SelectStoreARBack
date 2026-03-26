using FluentValidation;
using MediatR;
using SelectStoreAR.Application.DTOs;
using SelectStoreAR.Domain.Entities;
using SelectStoreAR.Domain.Interfaces;

namespace SelectStoreAR.Application.Commands.Categories;

public sealed record CreateCategoryCommand(
    string Name,
    Guid? ParentId,
    decimal? DefaultMarkup,
    int SortOrder = 0) : IRequest<CategoryDto>;

public sealed class CreateCategoryHandler(
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateCategoryCommand, CategoryDto>
{
    public async Task<CategoryDto> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        Category category = Category.Create(request.Name, request.ParentId, request.SortOrder);
        if (request.DefaultMarkup.HasValue)
        {
            category.SetDefaultMarkup(request.DefaultMarkup.Value);
        }

        categoryRepository.Add(category);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new CategoryDto(category.Id, category.Name, category.Slug.Value, category.ParentId, category.DefaultMarkup?.Percentage, category.SortOrder, category.ImageUrl, 0, []);
    }
}

public sealed class CreateCategoryValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre de la categoría es requerido")
            .MaximumLength(100).WithMessage("El nombre no puede superar 100 caracteres");

        RuleFor(x => x.DefaultMarkup)
            .InclusiveBetween(0, 500).WithMessage("El markup debe estar entre 0% y 500%")
            .When(x => x.DefaultMarkup.HasValue);

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0).WithMessage("El orden debe ser mayor o igual a cero");
    }
}
