using FluentValidation;
using SelectStoreAR.Application.Commands.Products;

namespace SelectStoreAR.Application.Validators;

public sealed class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre del producto es requerido")
            .MaximumLength(255).WithMessage("El nombre no puede superar 255 caracteres");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("La descripción es requerida");

        RuleFor(x => x.Brand)
            .NotEmpty().WithMessage("La marca es requerida")
            .MaximumLength(100).WithMessage("La marca no puede superar 100 caracteres");

        RuleFor(x => x.BasePriceUsd)
            .GreaterThan(0).WithMessage("El precio base debe ser mayor a cero");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("La categoría es requerida");

        RuleFor(x => x.MarkupPercentage)
            .InclusiveBetween(0, 500).WithMessage("El markup debe estar entre 0% y 500%")
            .When(x => x.MarkupPercentage.HasValue);
    }
}
