namespace ShopElegance.Core.DTOs;

// ─── Variant Image ────────────────────────────────────────────────────────

public class WhiteCoatVariantImageDto
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? AltText { get; set; }
    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }
}

// ─── Variant: List / Details / Customer / Preview ────────────────────────

public class WhiteCoatVariantListDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public string Color { get; set; } = "White";
    public string Fabric { get; set; } = string.Empty;
    public string CoatType { get; set; } = "Normal";
    public string SKU { get; set; } = string.Empty;
    public decimal FinalPrice { get; set; }
    public int StockQuantity { get; set; }
    public bool IsLowStock { get; set; }
    public bool IsOutOfStock { get; set; }
    public bool IsActive { get; set; }
    public string? PrimaryImageUrl { get; set; }
}

public class WhiteCoatVariantDetailDto : WhiteCoatVariantListDto
{
    public string? Barcode { get; set; }
    public decimal PriceAdjustment { get; set; }
    public double? Weight { get; set; }
    public int LowStockThreshold { get; set; }
    public List<WhiteCoatVariantImageDto> Images { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>What the storefront shows to a shopper picking options before adding to cart.</summary>
public class WhiteCoatCustomerOptionsDto
{
    public int ProductId { get; set; }
    public List<WhiteCoatVariantDetailDto> Variants { get; set; } = new();
    public List<EmbroideryFontDto> AvailableFonts { get; set; } = new();
    public List<LogoDto> AvailableLogos { get; set; } = new();
    public List<string> AvailableEmbroideryPositions { get; set; } = new();
}

public class WhiteCoatPreviewRequestDto
{
    public int WhiteCoatVariantId { get; set; }
    public bool EmbroideryEnabled { get; set; }
    public string? StudentName { get; set; }
    public string? DoctorName { get; set; }
    public string? Faculty { get; set; }
    public string? Department { get; set; }
    public int? GraduationYear { get; set; }
    public string? EmbroideryTextColor { get; set; }
    public int? EmbroideryFontId { get; set; }
    public string? EmbroideryPosition { get; set; }
    public string? LogoType { get; set; }
    public int? LogoId { get; set; }
    public string? CustomLogoUrl { get; set; }
    public bool RequiresSpecialPackaging { get; set; }
    public bool IsFragile { get; set; }
}

public class WhiteCoatPreviewResultDto
{
    public string BaseImageUrl { get; set; } = string.Empty;
    public string? LogoImageUrl { get; set; }
    public string EmbroideryPosition { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public decimal CustomizationFee { get; set; }
    public decimal FinalPrice { get; set; }
}

// ─── Variant: Create / Update (Admin) ─────────────────────────────────────

public class CreateWhiteCoatVariantDto
{
    public int ProductId { get; set; }
    public string Size { get; set; } = "Medium";
    public string Color { get; set; } = "White";
    public string Fabric { get; set; } = "Cotton";
    public string CoatType { get; set; } = "Normal";
    public string SKU { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public decimal PriceAdjustment { get; set; }
    public double? Weight { get; set; }
    public int StockQuantity { get; set; }
    public int LowStockThreshold { get; set; } = 5;
    public bool IsActive { get; set; } = true;
}

public class UpdateWhiteCoatVariantDto : CreateWhiteCoatVariantDto
{
}

public class WhiteCoatVariantAdminDto : WhiteCoatVariantDetailDto
{
    public int TimesOrdered { get; set; }
}

public class BulkUpdateWhiteCoatVariantDto
{
    public List<int> VariantIds { get; set; } = new();
    public decimal? PriceAdjustment { get; set; }
    public int? StockQuantity { get; set; }
    public bool? IsActive { get; set; }
}

public class WhiteCoatVariantFilterDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int? ProductId { get; set; }
    public string? Search { get; set; }
    public string? Size { get; set; }
    public string? Color { get; set; }
    public bool? IsActive { get; set; }
    public bool? LowStockOnly { get; set; }
    public string? SortBy { get; set; }
}

// ─── Embroidery Font / Logo catalog (Admin) ───────────────────────────────

public class EmbroideryFontDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CssFontFamily { get; set; } = string.Empty;
    public string? PreviewImageUrl { get; set; }
    public bool IsActive { get; set; }
}

public class CreateEmbroideryFontDto
{
    public string Name { get; set; } = string.Empty;
    public string CssFontFamily { get; set; } = string.Empty;
    public string? PreviewImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
}

public class LogoDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class CreateLogoDto
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "University";
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

// ─── Customization (Cart / Order) ─────────────────────────────────────────

public class CustomizationDto
{
    public int Id { get; set; }
    public int? WhiteCoatVariantId { get; set; }
    public bool EmbroideryEnabled { get; set; }
    public string? StudentName { get; set; }
    public string? DoctorName { get; set; }
    public string? Faculty { get; set; }
    public string? Department { get; set; }
    public int? GraduationYear { get; set; }
    public string? EmbroideryTextColor { get; set; }
    public int? EmbroideryFontId { get; set; }
    public string? EmbroideryFontName { get; set; }
    public string? EmbroideryPosition { get; set; }
    public string? LogoType { get; set; }
    public int? LogoId { get; set; }
    public string? LogoImageUrl { get; set; }
    public string? CustomLogoUrl { get; set; }
    public string? PreviewImageUrl { get; set; }
    public string CoatType { get; set; } = "Normal";
    public string Size { get; set; } = string.Empty;
    public string Fabric { get; set; } = string.Empty;
    public string Color { get; set; } = "White";
    public bool RequiresSpecialPackaging { get; set; }
    public bool IsFragile { get; set; }
    public decimal PriceAdjustment { get; set; }
    public decimal EmbroideryCost { get; set; }
    public decimal LogoCost { get; set; }
}

public class CreateCustomizationDto
{
    public int? WhiteCoatVariantId { get; set; }
    public bool EmbroideryEnabled { get; set; }

    public string? StudentName { get; set; }
    public string? DoctorName { get; set; }
    public string? Faculty { get; set; }
    public string? Department { get; set; }
    public int? GraduationYear { get; set; }
    public string? EmbroideryTextColor { get; set; }
    public int? EmbroideryFontId { get; set; }
    public string? EmbroideryPosition { get; set; }
    public string? LogoType { get; set; }
    public int? LogoId { get; set; }
    public string? CustomLogoUrl { get; set; }
    public bool RequiresSpecialPackaging { get; set; }
    public bool IsFragile { get; set; }
    public string CoatType { get; set; } = "Normal";
    public string Size { get; set; } = string.Empty;
    public string Fabric { get; set; } = string.Empty;
    public string Color { get; set; } = "White";
}

/// <summary>Add-to-cart request for a customizable White Coat product.</summary>
public class AddCustomizedItemToCartDto
{
    public int ProductId { get; set; }
    public int Quantity { get; set; } = 1;
    public CreateCustomizationDto Customization { get; set; } = new();
}

// ─── Shipping Customization ───────────────────────────────────────────────

public class ShippingCalculationResultDto
{
    public string Governorate { get; set; } = string.Empty;
    public decimal ShippingCost { get; set; }
    public int EstimatedDeliveryDays { get; set; }
    public bool IsValid { get; set; } = true;
    public string? ErrorMessage { get; set; }
    public bool IsFreeLocalDelivery { get; set; }
}

public class ShippingCustomizationDto
{
    public int Id { get; set; }
    public int CustomizationId { get; set; }
    public bool RequiresSpecialPackaging { get; set; }
    public bool IsFragile { get; set; }
    public string? HandlingInstructions { get; set; }
    public int EstimatedExtraDays { get; set; }
    public decimal ExtraShippingFee { get; set; }
}

public class CreateShippingCustomizationDto
{
    public int CustomizationId { get; set; }
    public bool RequiresSpecialPackaging { get; set; }
    public bool IsFragile { get; set; }
    public string? HandlingInstructions { get; set; }
    public int EstimatedExtraDays { get; set; }
    public decimal ExtraShippingFee { get; set; }
}

// ─── Customization History ────────────────────────────────────────────────

public class CustomizationHistoryDto
{
    public int Id { get; set; }
    public string ChangedField { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? ChangedByUserId { get; set; }
    public DateTime ChangedAt { get; set; }
}
