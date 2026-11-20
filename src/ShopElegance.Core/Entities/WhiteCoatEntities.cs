using ShopElegance.Core.Enums;

namespace ShopElegance.Core.Entities;

/// <summary>
/// A purchasable variant of a white coat Product (size/color/fabric/etc).
/// Inventory, SKU, and pricing all live at the variant level.
/// </summary>
public class WhiteCoatVariant : BaseEntity
{
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    // New required fields replacing old ones
    public string CoatType { get; set; } = "Normal";
    public string Size { get; set; } = "Medium";
    public string Fabric { get; set; } = "Cotton";
    public string Color { get; set; } = "White";

    public string SKU { get; set; } = string.Empty;
    public string? Barcode { get; set; }

    /// <summary>Added on top of the base Product.Price for this specific variant.</summary>
    public decimal PriceAdjustment { get; set; }
    public double? Weight { get; set; }

    public int StockQuantity { get; set; }
    public int LowStockThreshold { get; set; } = 5;
    public bool IsActive { get; set; } = true;

    /// <summary>Optimistic concurrency token so two simultaneous checkouts can't both oversell the last unit.</summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<WhiteCoatVariantImage> Images { get; set; } = new List<WhiteCoatVariantImage>();
    public ICollection<Customization> Customizations { get; set; } = new List<Customization>();
}

public class WhiteCoatVariantImage : BaseEntity
{
    public int WhiteCoatVariantId { get; set; }
    public WhiteCoatVariant WhiteCoatVariant { get; set; } = null!;

    public string Url { get; set; } = string.Empty;
    public string? AltText { get; set; }
    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>Catalog of fonts admins make available for embroidery text.</summary>
public class EmbroideryFont : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string CssFontFamily { get; set; } = string.Empty;
    public string? PreviewImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>Catalog of pre-approved logos (university / hospital) admins upload.</summary>
public class Logo : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public LogoSourceType Type { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// A snapshot of one customer's customization choices for a single WhiteCoatVariant.
/// Linked to exactly one CartItem (while shopping) or one OrderItem (once purchased) — never both.
/// </summary>
public class Customization : BaseEntity
{
    public int? WhiteCoatVariantId { get; set; }
    public WhiteCoatVariant? WhiteCoatVariant { get; set; }

    public bool EmbroideryEnabled { get; set; }
    public string? StudentName { get; set; }
    public string? DoctorName { get; set; }
    public string? Faculty { get; set; }
    public string? Department { get; set; }
    public int? GraduationYear { get; set; }
    public string? EmbroideryTextColor { get; set; } // hex, e.g. #1A1A1A
    public int? EmbroideryFontId { get; set; }
    public EmbroideryFont? EmbroideryFont { get; set; }
    public EmbroideryPosition? EmbroideryPosition { get; set; }

    public LogoSourceType? LogoType { get; set; }
    public int? LogoId { get; set; } // set when LogoType is University/Hospital (pre-approved catalog)
    public Logo? Logo { get; set; }
    public string? CustomLogoUrl { get; set; } // set when LogoType == Custom (customer upload)

    public string? PreviewImageUrl { get; set; }

    public string CoatType { get; set; } = "Normal"; // Normal / Zip
    public string Size { get; set; } = string.Empty; // Medium, Large, X-Large, 2X-Large, 3X-Large
    public string Fabric { get; set; } = string.Empty; // Cotton / Regular
    public string Color { get; set; } = "White"; // Fixed to "White"

    /// <summary>Extra fee charged for embroidery/logo work, snapshotted at time of selection.</summary>
    public decimal PriceAdjustment { get; set; }
    public decimal EmbroideryCost { get; set; }
    public decimal LogoCost { get; set; }

    public ICollection<CustomizationHistory> History { get; set; } = new List<CustomizationHistory>();
    public ShippingCustomization? ShippingCustomization { get; set; }
}

/// <summary>
/// Audit trail of every change made to a Customization — e.g. an admin correcting a misspelled
/// embroidery name, or a customer revising their choice before checkout.
/// </summary>
public class CustomizationHistory : BaseEntity
{
    public int CustomizationId { get; set; }
    public Customization Customization { get; set; } = null!;

    public string ChangedField { get; set; } = string.Empty; // e.g. "StudentName", "LogoId", "EmbroideryPosition"
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? ChangedByUserId { get; set; } // null when the customer themselves made the change
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Special shipping handling required because of a customization — e.g. embroidered coats need
/// extra packaging/time, or a fragile custom logo print needs careful handling.
/// One-to-one with a Customization; absent means standard shipping applies.
/// </summary>
public class ShippingCustomization : BaseEntity
{
    public int CustomizationId { get; set; }
    public Customization Customization { get; set; } = null!;

    public bool RequiresSpecialPackaging { get; set; }
    public bool IsFragile { get; set; }
    public string? HandlingInstructions { get; set; }
    public int EstimatedExtraDays { get; set; }
    public decimal ExtraShippingFee { get; set; }
}
