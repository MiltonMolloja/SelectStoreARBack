using FluentValidation;
using SelectStoreAR.Application.Commands.ExchangeRates;

namespace SelectStoreAR.Application.Validators;

public sealed class UpdateExchangeRateValidator : AbstractValidator<UpdateExchangeRateCommand>
{
    public UpdateExchangeRateValidator()
    {
        RuleFor(x => x.Rate)
            .GreaterThan(0).WithMessage("La cotización debe ser mayor a cero")
            .LessThan(100_000).WithMessage("La cotización no puede superar $100.000");

        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("El tipo de cotización es requerido")
            .Must(t => t is "blue" or "oficial" or "cripto")
            .WithMessage("El tipo debe ser: blue, oficial o cripto");
    }
}
