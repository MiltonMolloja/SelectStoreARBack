using FluentValidation;
using SelectStoreAR.Application.Commands.Orders;

namespace SelectStoreAR.Application.Validators;

public sealed class UpdateOrderStatusValidator : AbstractValidator<UpdateOrderStatusCommand>
{
    private static readonly string[] ValidStatuses =
    [
        "deposited", "orderedfromsupplier", "intransit",
        "readyfordelivery", "delivered", "cancelled",
    ];

    public UpdateOrderStatusValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty().WithMessage("El ID del pedido es requerido");

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("El estado es requerido")
            .Must(s => ValidStatuses.Contains(s.ToLowerInvariant()))
            .WithMessage($"Estado inválido. Valores válidos: {string.Join(", ", ValidStatuses)}");

        RuleFor(x => x.DepositType)
            .NotEmpty().WithMessage("El tipo de depósito es requerido al confirmar la seña")
            .When(x => x.Status.Equals("deposited", StringComparison.OrdinalIgnoreCase));

        RuleFor(x => x.DepositType)
            .Must(t => t is "persona" or "transferencia")
            .WithMessage("El tipo de depósito debe ser 'persona' o 'transferencia'")
            .When(x => x.Status.Equals("deposited", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrEmpty(x.DepositType));
    }
}
