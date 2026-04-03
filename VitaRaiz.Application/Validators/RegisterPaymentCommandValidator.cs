using FluentValidation;
using VitaRaiz.Application.Commands.Payments;

namespace VitaRaiz.Application.Validators;

public class RegisterPaymentCommandValidator : AbstractValidator<RegisterPaymentCommand>
{
    public RegisterPaymentCommandValidator()
    {
        RuleFor(x => x.SaleId)
            .GreaterThan(0)
            .WithMessage("El ID de venta debe ser mayor a 0");

        RuleFor(x => x.CollectorId)
            .GreaterThan(0)
            .WithMessage("El ID del cobrador debe ser mayor a 0");

        RuleFor(x => x.Amount)
            .GreaterThanOrEqualTo(0)
            .WithMessage("El monto no puede ser negativo");

        RuleFor(x => x.GpsLatitude)
            .InclusiveBetween(-90, 90)
            .When(x => x.GpsLatitude.HasValue)
            .WithMessage("La latitud debe estar entre -90 y 90");

        RuleFor(x => x.GpsLongitude)
            .InclusiveBetween(-180, 180)
            .When(x => x.GpsLongitude.HasValue)
            .WithMessage("La longitud debe estar entre -180 y 180");
    }
}
