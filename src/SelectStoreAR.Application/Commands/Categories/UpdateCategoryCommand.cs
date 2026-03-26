using MediatR;
using SelectStoreAR.Application.DTOs;
using SelectStoreAR.Domain.Common;
using SelectStoreAR.Domain.Entities;
using SelectStoreAR.Domain.Interfaces;

namespace SelectStoreAR.Application.Commands.Categories;

public sealed record UpdateCategoryCommand(
    Guid Id,
    string Name,
    Guid? ParentId,
    decimal? DefaultMarkup,
    int SortOrder) : IRequest<CategoryDto>;

public sealed class UpdateCategoryHandler(
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateCategoryCommand, CategoryDto>
{
    public async Task<CategoryDto> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        Category category = await categoryRepository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new DomainException($"Categoría '{request.Id}' no encontrada");

        category.Update(request.Name, request.ParentId, request.SortOrder);
        category.SetDefaultMarkup(request.DefaultMarkup);

        categoryRepository.Update(category);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new CategoryDto(category.Id, category.Name, category.Slug.Value, category.ParentId, category.DefaultMarkup?.Percentage, category.SortOrder, category.ImageUrl, category.Products.Count, []);
    }
}
