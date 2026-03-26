using MediatR;
using SelectStoreAR.Domain.Common;
using SelectStoreAR.Domain.Entities;
using SelectStoreAR.Domain.Interfaces;

namespace SelectStoreAR.Application.Commands.Categories;

public sealed record DeleteCategoryCommand(Guid Id) : IRequest;

public sealed class DeleteCategoryHandler(
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteCategoryCommand>
{
    public async Task Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        Category category = await categoryRepository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new DomainException($"Categoría '{request.Id}' no encontrada");

        if (category.Products.Count > 0)
        {
            throw new DomainException("No se puede eliminar una categoría que tiene productos asociados");
        }

        if (category.Children.Count > 0)
        {
            throw new DomainException("No se puede eliminar una categoría que tiene subcategorías");
        }

        categoryRepository.Remove(category);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
