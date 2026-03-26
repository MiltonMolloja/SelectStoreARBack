using FluentValidation;
using SelectStoreAR.Application.Commands.Orders;

namespace SelectStoreAR.Application.Validators;

public sealed class PlaceOrderValidator : AbstractValidator<PlaceOrderCommand>
{
    public PlaceOrderValidator()
    {
        RuleFor(x => x.CustomerName)
            .NotEmpty().WithMessage("El nombre del cliente es requerido")
            .MaximumLength(200).WithMessage("El nombre no puede superar 200 caracteres");

        RuleFor(x => x.CustomerPhone)
            .NotEmpty().WithMessage("El teléfono del cliente es requerido")
            .Matches(@"^\+?[\d\s\-\(\)]{8,20}$").WithMessage("El formato del teléfono no es válido");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("El pedido debe tener al menos un producto");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId)
                .NotEmpty().WithMessage("El ID del producto es requerido");

            item.RuleFor(i => i.Quantity)
                .GreaterThan(0).WithMessage("La cantidad debe ser mayor a cero")
                .LessThanOrEqualTo(99).WithMessage("La cantidad no puede superar 99 unidades");
        });
    }
}
