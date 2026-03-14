using Ecommerce.Application.Modules.Products.DTOs;
using FluentValidation;

namespace Ecommerce.Application.Modules.Products.Validators;

public class CreateProductValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
        RuleFor(x => x.BasePrice).GreaterThanOrEqualTo(0).When(x => x.BasePrice.HasValue);
        RuleFor(x => x.Description).MaximumLength(5000).When(x => x.Description is not null);
        RuleFor(x => x.Brand).MaximumLength(150).When(x => x.Brand is not null);
    }
}

public class CreateVariantValidator : AbstractValidator<CreateVariantRequest>
{
    public CreateVariantValidator()
    {
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0).When(x => x.Price.HasValue);
        RuleFor(x => x.InitialStock).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Color).MaximumLength(50).When(x => x.Color is not null);
        RuleFor(x => x.Size).MaximumLength(50).When(x => x.Size is not null);
    }
}
