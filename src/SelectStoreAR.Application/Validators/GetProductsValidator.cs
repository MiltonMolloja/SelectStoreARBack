using FluentValidation;
using SelectStoreAR.Application.Queries.Products;

namespace SelectStoreAR.Application.Validators;

public sealed class GetProductsValidator : AbstractValidator<GetProductsQuery>
{
    public GetProductsValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("La página debe ser mayor a cero");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 50).WithMessage("El tamaño de página debe estar entre 1 y 50");

        RuleFor(x => x.MinPriceUsd)
            .GreaterThanOrEqualTo(0).WithMessage("El precio mínimo no puede ser negativo")
            .When(x => x.MinPriceUsd.HasValue);

        RuleFor(x => x.MaxPriceUsd)
            .GreaterThan(0).WithMessage("El precio máximo debe ser mayor a cero")
            .GreaterThan(x => x.MinPriceUsd ?? 0).WithMessage("El precio máximo debe ser mayor al mínimo")
            .When(x => x.MaxPriceUsd.HasValue);
    }
}
