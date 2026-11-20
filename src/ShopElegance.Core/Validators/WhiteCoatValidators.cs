using FluentValidation;
using ShopElegance.Core.DTOs;

namespace ShopElegance.Core.Validators;

// ─── White Coat Variant Validators ───────────────────────────────────────

public class CreateWhiteCoatVariantValidator : AbstractValidator<CreateWhiteCoatVariantDto>
{
    private static readonly string[] ValidSizes = { "Medium", "Large", "X-Large", "2X-Large", "3X-Large" };
    private static readonly string[] ValidCoatTypes = { "Normal", "Zip" };
    private static readonly string[] ValidFabrics = { "Cotton", "Regular" };

    public CreateWhiteCoatVariantValidator()
    {
        RuleFor(x => x.ProductId)
            .GreaterThan(0).WithMessage("A valid product is required.");

        RuleFor(x => x.Size)
            .Must(s => ValidSizes.Contains(s)).WithMessage("Size must be one of: " + string.Join(", ", ValidSizes));

        RuleFor(x => x.CoatType)
            .Must(s => ValidCoatTypes.Contains(s)).WithMessage("Coat type must be one of: " + string.Join(", ", ValidCoatTypes));

        RuleFor(x => x.Fabric)
            .Must(s => ValidFabrics.Contains(s)).WithMessage("Fabric must be one of: " + string.Join(", ", ValidFabrics));

        RuleFor(x => x.Color)
            .NotEmpty().WithMessage("Color is required.")
            .MaximumLength(50);

        RuleFor(x => x.SKU)
            .NotEmpty().WithMessage("SKU is required.")
            .MaximumLength(100);

        RuleFor(x => x.Barcode)
            .MaximumLength(100);

        RuleFor(x => x.PriceAdjustment)
            .GreaterThanOrEqualTo(0).WithMessage("Price adjustment cannot be negative.");

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Stock quantity cannot be negative.");

        RuleFor(x => x.LowStockThreshold)
            .GreaterThanOrEqualTo(0).WithMessage("Low stock threshold cannot be negative.");
    }
}

public class UpdateWhiteCoatVariantValidator : AbstractValidator<UpdateWhiteCoatVariantDto>
{
    public UpdateWhiteCoatVariantValidator()
    {
        Include(new CreateWhiteCoatVariantValidator());
    }
}

// ─── Embroidery Font / Logo Validators ───────────────────────────────────

public class CreateEmbroideryFontValidator : AbstractValidator<CreateEmbroideryFontDto>
{
    public CreateEmbroideryFontValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.CssFontFamily).NotEmpty().MaximumLength(200);
    }
}

public class CreateLogoValidator : AbstractValidator<CreateLogoDto>
{
    private static readonly string[] ValidTypes = { "University", "Hospital", "Custom" };

    public CreateLogoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Type).Must(t => ValidTypes.Contains(t))
            .WithMessage("Type must be one of: " + string.Join(", ", ValidTypes));
        RuleFor(x => x.ImageUrl).NotEmpty().WithMessage("Logo image is required.");
    }
}

// ─── Customization Validators ────────────────────────────────────────────

public class CreateCustomizationValidator : AbstractValidator<CreateCustomizationDto>
{
    private static readonly string[] ValidPositions = { "LeftChest", "RightChest", "Sleeve", "Back" };
    private static readonly string[] ValidLogoTypes = { "University", "Hospital", "Custom" };

    public CreateCustomizationValidator()
    {
        // Variant ID is optional; it's only needed if a specific SKU/variant is chosen.
        
        When(x => x.EmbroideryEnabled, () =>
        {
            RuleFor(x => x)
                .Must(x => !string.IsNullOrWhiteSpace(x.StudentName) || !string.IsNullOrWhiteSpace(x.DoctorName))
                .WithMessage("Either a student name or doctor name is required when embroidery is enabled.");

            RuleFor(x => x.Faculty).MaximumLength(150);
            RuleFor(x => x.Department).MaximumLength(150);

            RuleFor(x => x.GraduationYear)
                .InclusiveBetween(1950, 2100).When(x => x.GraduationYear.HasValue)
                .WithMessage("Graduation year must be realistic.");

            RuleFor(x => x.EmbroideryTextColor)
                .Matches("^#[0-9A-Fa-f]{6}$").When(x => !string.IsNullOrEmpty(x.EmbroideryTextColor))
                .WithMessage("Embroidery text color must be a valid hex code, e.g. #1A1A1A.");

            RuleFor(x => x.EmbroideryFontId)
                // EmbroideryFontId is `int?`. FluentValidation's GreaterThan() silently treats a
                // null value as valid (comparison validators skip nulls) — that let EmbroideryEnabled
                // = true through with no font selected. Must() checks HasValue explicitly first.
                .Must(id => id.HasValue && id.Value > 0)
                .WithMessage("A font must be selected when embroidery is enabled.");

            RuleFor(x => x.EmbroideryPosition)
                .NotEmpty().Must(p => ValidPositions.Contains(p))
                .WithMessage("Embroidery position must be one of: " + string.Join(", ", ValidPositions));
        });

        When(x => !string.IsNullOrEmpty(x.LogoType), () =>
        {
            RuleFor(x => x.LogoType)
                .Must(t => ValidLogoTypes.Contains(t!))
                .WithMessage("Logo type must be one of: " + string.Join(", ", ValidLogoTypes));

            RuleFor(x => x.LogoId)
                // Same null-skip gap as EmbroideryFontId above: LogoId is `int?`, so GreaterThan(0)
                // alone passed a University/Hospital selection through with no LogoId at all.
                .Must(id => id.HasValue && id.Value > 0)
                .When(x => x.LogoType is "University" or "Hospital")
                .WithMessage("A catalog logo must be selected for University/Hospital logo type.");

            RuleFor(x => x.CustomLogoUrl)
                .NotEmpty()
                .When(x => x.LogoType == "Custom")
                .WithMessage("A custom logo upload is required for Custom logo type.");
        });
    }
}

public class AddCustomizedItemToCartValidator : AbstractValidator<AddCustomizedItemToCartDto>
{
    public AddCustomizedItemToCartValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThan(0).LessThanOrEqualTo(100);
        RuleFor(x => x.Customization).NotNull().SetValidator(new CreateCustomizationValidator());
    }
}
