using Microsoft.Extensions.Logging;
using ShopElegance.Core.DTOs;
using ShopElegance.Core.Entities;
using ShopElegance.Core.Enums;
using ShopElegance.Core.Interfaces.Repositories;
using ShopElegance.Core.Interfaces.Services;

namespace ShopElegance.Infrastructure.Services;

public class WhiteCoatVariantService : IWhiteCoatVariantService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<WhiteCoatVariantService> _logger;

    public WhiteCoatVariantService(IUnitOfWork uow, ILogger<WhiteCoatVariantService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<WhiteCoatVariantDetailDto?> GetVariantByIdAsync(int id)
    {
        var variant = await _uow.WhiteCoatVariants.GetWithDetailsAsync(id);
        return variant == null ? null : MapToDetailDto(variant);
    }

    public async Task<WhiteCoatCustomerOptionsDto> GetCustomerOptionsAsync(int productId)
    {
        var variants = await _uow.WhiteCoatVariants.GetByProductIdAsync(productId, activeOnly: true);
        var fonts = await _uow.EmbroideryFonts.GetActiveAsync();
        var logos = await _uow.Logos.GetActiveAsync();

        return new WhiteCoatCustomerOptionsDto
        {
            ProductId = productId,
            Variants = variants.Select(MapToDetailDto).ToList(),
            AvailableFonts = fonts.Select(MapFontDto).ToList(),
            AvailableLogos = logos.Select(MapLogoDto).ToList(),
            AvailableEmbroideryPositions = Enum.GetNames<EmbroideryPosition>().ToList()
        };
    }

    public async Task<PagedResultDto<WhiteCoatVariantAdminDto>> GetVariantsAsync(WhiteCoatVariantFilterDto filter)
    {
        var (variants, total) = await _uow.WhiteCoatVariants.GetPagedAsync(
            filter.Page, filter.PageSize, filter.ProductId, filter.Search,
            filter.Size, filter.Color, filter.IsActive, filter.LowStockOnly, filter.SortBy);

        return new PagedResultDto<WhiteCoatVariantAdminDto>
        {
            Items = variants.Select(v =>
            {
                var dto = MapToDetailDto(v);
                return new WhiteCoatVariantAdminDto
                {
                    Id = dto.Id,
                    ProductId = dto.ProductId,
                    ProductName = dto.ProductName,
                    Size = dto.Size,
                    Color = dto.Color,
                    Fabric = dto.Fabric,
                    CoatType = dto.CoatType,
                    SKU = dto.SKU,
                    FinalPrice = dto.FinalPrice,
                    StockQuantity = dto.StockQuantity,
                    IsLowStock = dto.IsLowStock,
                    IsOutOfStock = dto.IsOutOfStock,
                    IsActive = dto.IsActive,
                    PrimaryImageUrl = dto.PrimaryImageUrl,
                    Barcode = dto.Barcode,
                    PriceAdjustment = dto.PriceAdjustment,
                    Weight = dto.Weight,
                    LowStockThreshold = dto.LowStockThreshold,
                    Images = dto.Images,
                    CreatedAt = dto.CreatedAt,
                    UpdatedAt = dto.UpdatedAt,
                    TimesOrdered = 0 
                };
            }),
            TotalCount = total,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<IEnumerable<WhiteCoatVariantListDto>> GetLowStockVariantsAsync()
    {
        var variants = await _uow.WhiteCoatVariants.GetLowStockAsync();
        return variants.Select(v => (WhiteCoatVariantListDto)MapToDetailDto(v));
    }

    public async Task<WhiteCoatVariantDetailDto> CreateVariantAsync(CreateWhiteCoatVariantDto dto)
    {
        if (await _uow.WhiteCoatVariants.SkuExistsAsync(dto.SKU))
            throw new InvalidOperationException($"SKU '{dto.SKU}' is already in use.");

        var variant = new WhiteCoatVariant
        {
            ProductId = dto.ProductId,
            CoatType = dto.CoatType,
            Size = dto.Size,
            Color = dto.Color,
            Fabric = dto.Fabric,
            SKU = dto.SKU,
            Barcode = dto.Barcode,
            PriceAdjustment = dto.PriceAdjustment,
            Weight = dto.Weight,
            StockQuantity = dto.StockQuantity,
            LowStockThreshold = dto.LowStockThreshold,
            IsActive = dto.IsActive
        };

        await _uow.WhiteCoatVariants.AddAsync(variant);
        await _uow.SaveChangesAsync();
        _logger.LogInformation("Created White Coat variant {Sku} for product {ProductId}", variant.SKU, variant.ProductId);

        var created = await _uow.WhiteCoatVariants.GetWithDetailsAsync(variant.Id);
        return MapToDetailDto(created!);
    }

    public async Task<WhiteCoatVariantDetailDto> UpdateVariantAsync(int id, UpdateWhiteCoatVariantDto dto)
    {
        var variant = await _uow.WhiteCoatVariants.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"White Coat variant {id} not found.");

        if (await _uow.WhiteCoatVariants.SkuExistsAsync(dto.SKU, id))
            throw new InvalidOperationException($"SKU '{dto.SKU}' is already in use.");

        variant.CoatType = dto.CoatType;
        variant.Size = dto.Size;
        variant.Color = dto.Color;
        variant.Fabric = dto.Fabric;
        variant.SKU = dto.SKU;
        variant.Barcode = dto.Barcode;
        variant.PriceAdjustment = dto.PriceAdjustment;
        variant.Weight = dto.Weight;
        variant.StockQuantity = dto.StockQuantity;
        variant.LowStockThreshold = dto.LowStockThreshold;
        variant.IsActive = dto.IsActive;

        _uow.WhiteCoatVariants.Update(variant);
        await _uow.SaveChangesAsync();

        var updated = await _uow.WhiteCoatVariants.GetWithDetailsAsync(id);
        return MapToDetailDto(updated!);
    }

    public async Task DeleteVariantAsync(int id)
    {
        var variant = await _uow.WhiteCoatVariants.GetByIdAsync(id);
        if (variant == null) return;
        variant.IsDeleted = true;
        variant.DeletedAt = DateTime.UtcNow;
        _uow.WhiteCoatVariants.Update(variant);
        await _uow.SaveChangesAsync();
    }

    public async Task BulkUpdateAsync(BulkUpdateWhiteCoatVariantDto dto)
    {
        foreach (var variantId in dto.VariantIds)
        {
            var variant = await _uow.WhiteCoatVariants.GetByIdAsync(variantId);
            if (variant == null) continue;

            if (dto.PriceAdjustment.HasValue) variant.PriceAdjustment = dto.PriceAdjustment.Value;
            if (dto.StockQuantity.HasValue) variant.StockQuantity = dto.StockQuantity.Value;
            if (dto.IsActive.HasValue) variant.IsActive = dto.IsActive.Value;

            _uow.WhiteCoatVariants.Update(variant);
        }
        await _uow.SaveChangesAsync();
    }

    public async Task<WhiteCoatVariantImageDto> AddImageAsync(int variantId, string url, string? altText, bool isPrimary)
    {
        var variant = await _uow.WhiteCoatVariants.GetWithDetailsAsync(variantId)
            ?? throw new KeyNotFoundException($"White Coat variant {variantId} not found.");

        if (isPrimary)
            foreach (var img in variant.Images) img.IsPrimary = false;

        var image = new WhiteCoatVariantImage
        {
            WhiteCoatVariantId = variantId,
            Url = url,
            AltText = altText,
            IsPrimary = isPrimary || !variant.Images.Any(),
            SortOrder = variant.Images.Count
        };

        variant.Images.Add(image);
        await _uow.SaveChangesAsync();

        return new WhiteCoatVariantImageDto
        {
            Id = image.Id,
            Url = image.Url,
            AltText = image.AltText,
            IsPrimary = image.IsPrimary,
            SortOrder = image.SortOrder
        };
    }

    public async Task RemoveImageAsync(int imageId)
    {
        // Images are exposed only via their parent variant's collection.
        var variant = await _uow.WhiteCoatVariants.GetByImageIdAsync(imageId);
        var image = variant?.Images.FirstOrDefault(i => i.Id == imageId);
        if (image == null) return;

        variant!.Images.Remove(image);
        await _uow.SaveChangesAsync();
    }

    private static WhiteCoatVariantDetailDto MapToDetailDto(WhiteCoatVariant v) => new()
    {
        Id = v.Id,
        ProductId = v.ProductId,
        ProductName = v.Product?.Name ?? string.Empty,
        Size = v.Size,
        Color = v.Color,
        Fabric = v.Fabric,
        CoatType = v.CoatType,
        SKU = v.SKU,
        FinalPrice = (v.Product?.Price ?? 0) + v.PriceAdjustment,
        StockQuantity = v.StockQuantity,
        IsLowStock = v.StockQuantity > 0 && v.StockQuantity <= v.LowStockThreshold,
        IsOutOfStock = v.StockQuantity <= 0,
        IsActive = v.IsActive,
        PrimaryImageUrl = v.Images.FirstOrDefault(i => i.IsPrimary)?.Url ?? v.Images.FirstOrDefault()?.Url,
        Barcode = v.Barcode,
        PriceAdjustment = v.PriceAdjustment,
        Weight = v.Weight,
        LowStockThreshold = v.LowStockThreshold,
        Images = v.Images.OrderBy(i => i.SortOrder).Select(i => new WhiteCoatVariantImageDto
        {
            Id = i.Id,
            Url = i.Url,
            AltText = i.AltText,
            IsPrimary = i.IsPrimary,
            SortOrder = i.SortOrder
        }).ToList(),
        CreatedAt = v.CreatedAt,
        UpdatedAt = v.UpdatedAt
    };

    private static EmbroideryFontDto MapFontDto(EmbroideryFont f) => new()
    {
        Id = f.Id,
        Name = f.Name,
        CssFontFamily = f.CssFontFamily,
        PreviewImageUrl = f.PreviewImageUrl,
        IsActive = f.IsActive
    };

    private static LogoDto MapLogoDto(Logo l) => new()
    {
        Id = l.Id,
        Name = l.Name,
        Type = l.Type.ToString(),
        ImageUrl = l.ImageUrl,
        IsActive = l.IsActive
    };
}

public class EmbroideryService : IEmbroideryService
{
    private readonly IUnitOfWork _uow;

    public EmbroideryService(IUnitOfWork uow) => _uow = uow;

    public async Task<IEnumerable<EmbroideryFontDto>> GetActiveFontsAsync()
    {
        var fonts = await _uow.EmbroideryFonts.GetActiveAsync();
        return fonts.Select(MapDto);
    }

    public async Task<IEnumerable<EmbroideryFontDto>> GetAllFontsAsync()
    {
        var fonts = await _uow.EmbroideryFonts.GetAllAsync();
        return fonts.Select(MapDto);
    }

    public async Task<EmbroideryFontDto> CreateFontAsync(CreateEmbroideryFontDto dto)
    {
        var font = new EmbroideryFont
        {
            Name = dto.Name,
            CssFontFamily = dto.CssFontFamily,
            PreviewImageUrl = dto.PreviewImageUrl,
            IsActive = dto.IsActive
        };
        await _uow.EmbroideryFonts.AddAsync(font);
        await _uow.SaveChangesAsync();
        return MapDto(font);
    }

    public async Task UpdateFontAsync(int id, CreateEmbroideryFontDto dto)
    {
        var font = await _uow.EmbroideryFonts.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Embroidery font {id} not found.");

        font.Name = dto.Name;
        font.CssFontFamily = dto.CssFontFamily;
        font.PreviewImageUrl = dto.PreviewImageUrl;
        font.IsActive = dto.IsActive;

        _uow.EmbroideryFonts.Update(font);
        await _uow.SaveChangesAsync();
    }

    public async Task DeleteFontAsync(int id)
    {
        var font = await _uow.EmbroideryFonts.GetByIdAsync(id);
        if (font == null) return;
        font.IsDeleted = true;
        font.DeletedAt = DateTime.UtcNow;
        _uow.EmbroideryFonts.Update(font);
        await _uow.SaveChangesAsync();
    }

    private static EmbroideryFontDto MapDto(EmbroideryFont f) => new()
    {
        Id = f.Id,
        Name = f.Name,
        CssFontFamily = f.CssFontFamily,
        PreviewImageUrl = f.PreviewImageUrl,
        IsActive = f.IsActive
    };
}

public class LogoService : ILogoService
{
    private readonly IUnitOfWork _uow;

    public LogoService(IUnitOfWork uow) => _uow = uow;

    public async Task<IEnumerable<LogoDto>> GetActiveLogosAsync(string? type = null)
    {
        LogoSourceType? typeEnum = string.IsNullOrEmpty(type) ? null : Enum.Parse<LogoSourceType>(type);
        var logos = await _uow.Logos.GetActiveAsync(typeEnum);
        return logos.Select(MapDto);
    }

    public async Task<IEnumerable<LogoDto>> GetAllLogosAsync()
    {
        var logos = await _uow.Logos.GetAllAsync();
        return logos.Select(MapDto);
    }

    public async Task<LogoDto> CreateLogoAsync(CreateLogoDto dto)
    {
        var logo = new Logo
        {
            Name = dto.Name,
            Type = Enum.Parse<LogoSourceType>(dto.Type),
            ImageUrl = dto.ImageUrl,
            IsActive = dto.IsActive
        };
        await _uow.Logos.AddAsync(logo);
        await _uow.SaveChangesAsync();
        return MapDto(logo);
    }

    public async Task UpdateLogoAsync(int id, CreateLogoDto dto)
    {
        var logo = await _uow.Logos.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Logo {id} not found.");

        logo.Name = dto.Name;
        logo.Type = Enum.Parse<LogoSourceType>(dto.Type);
        logo.ImageUrl = dto.ImageUrl;
        logo.IsActive = dto.IsActive;

        _uow.Logos.Update(logo);
        await _uow.SaveChangesAsync();
    }

    public async Task DeleteLogoAsync(int id)
    {
        var logo = await _uow.Logos.GetByIdAsync(id);
        if (logo == null) return;
        logo.IsDeleted = true;
        logo.DeletedAt = DateTime.UtcNow;
        _uow.Logos.Update(logo);
        await _uow.SaveChangesAsync();
    }

    private static LogoDto MapDto(Logo l) => new()
    {
        Id = l.Id,
        Name = l.Name,
        Type = l.Type.ToString(),
        ImageUrl = l.ImageUrl,
        IsActive = l.IsActive
    };
}

/// <summary>Fixed, transparent pricing rules for embroidery and logo add-ons.</summary>
public class PriceCalculationService : IPriceCalculationService
{
    private const decimal EmbroideryFee = 100.00m;
    private const decimal LogoFee = 100.00m;

    private readonly IUnitOfWork _uow;

    public PriceCalculationService(IUnitOfWork uow) => _uow = uow;

    public Task<(decimal EmbroideryCost, decimal LogoCost)> GetCustomizationCostsAsync(CreateCustomizationDto customization)
    {
        bool hasEmbroidery = !string.IsNullOrWhiteSpace(customization.StudentName) || 
            !string.IsNullOrWhiteSpace(customization.DoctorName) ||
            !string.IsNullOrWhiteSpace(customization.Faculty) || 
            !string.IsNullOrWhiteSpace(customization.Department) ||
            customization.GraduationYear.HasValue || 
            customization.EmbroideryFontId.HasValue ||
            !string.IsNullOrWhiteSpace(customization.EmbroideryTextColor) ||  
            !string.IsNullOrWhiteSpace(customization.EmbroideryPosition);
        
        bool hasLogo = (customization.LogoId.HasValue && customization.LogoId.Value != 0) || 
                       customization.LogoType == "Custom" || !string.IsNullOrWhiteSpace(customization.CustomLogoUrl);

        decimal totalFee = (hasEmbroidery || hasLogo) ? 150.00m : 0m;
        
        // Returning total fee as EmbroideryCost and 0 for LogoCost to keep the total flat 150
        return Task.FromResult((totalFee, 0m));
    }

    public Task<decimal> CalculateCustomizationFeeAsync(CreateCustomizationDto customization)
    {
        var costs = GetCustomizationCostsAsync(customization).Result;
        return Task.FromResult(costs.EmbroideryCost + costs.LogoCost);
    }

    public async Task<decimal> CalculateVariantFinalPriceAsync(int whiteCoatVariantId)
    {
        var variant = await _uow.WhiteCoatVariants.GetWithDetailsAsync(whiteCoatVariantId)
            ?? throw new KeyNotFoundException($"White Coat variant {whiteCoatVariantId} not found.");
        return (variant.Product?.Price ?? 0) + variant.PriceAdjustment;
    }
}

/// <summary>
/// Builds the data the storefront needs to render a live preview.
/// The actual image compositing happens client-side (JS layering base + logo + text);
/// this service just resolves URLs and computed pricing so the client has everything it needs.
/// </summary>
public class CustomizationPreviewService : ICustomizationPreviewService
{
    private readonly IUnitOfWork _uow;
    private readonly IPriceCalculationService _priceService;

    public CustomizationPreviewService(IUnitOfWork uow, IPriceCalculationService priceService)
    {
        _uow = uow;
        _priceService = priceService;
    }

    public async Task<WhiteCoatPreviewResultDto> GeneratePreviewAsync(WhiteCoatPreviewRequestDto request)
    {
        var variant = await _uow.WhiteCoatVariants.GetWithDetailsAsync(request.WhiteCoatVariantId)
            ?? throw new KeyNotFoundException($"White Coat variant {request.WhiteCoatVariantId} not found.");

        string? logoUrl = null;
        if (request.LogoType is "University" or "Hospital" && request.LogoId.HasValue)
        {
            var logo = await _uow.Logos.GetByIdAsync(request.LogoId.Value);
            logoUrl = logo?.ImageUrl;
        }
        else if (request.LogoType == "Custom")
        {
            logoUrl = request.CustomLogoUrl;
        }

        var customizationDto = new CreateCustomizationDto
        {
            WhiteCoatVariantId = request.WhiteCoatVariantId,
            EmbroideryEnabled = request.EmbroideryEnabled,
            StudentName = request.StudentName,
            DoctorName = request.DoctorName,
            Faculty = request.Faculty,
            Department = request.Department,
            GraduationYear = request.GraduationYear,
            LogoType = request.LogoType
        };

        var costs = await _priceService.GetCustomizationCostsAsync(customizationDto);
        var customizationFee = costs.EmbroideryCost + costs.LogoCost;
        var basePrice = (variant.Product?.Price ?? 0) + variant.PriceAdjustment;

        return new WhiteCoatPreviewResultDto
        {
            BaseImageUrl = variant.Images.FirstOrDefault(i => i.IsPrimary)?.Url ?? variant.Images.FirstOrDefault()?.Url ?? string.Empty,
            LogoImageUrl = logoUrl,
            EmbroideryPosition = request.EmbroideryPosition ?? string.Empty,
            BasePrice = basePrice,
            CustomizationFee = customizationFee,
            FinalPrice = basePrice + customizationFee
        };
    }
}

public class CustomizationService : ICustomizationService
{
    private readonly IUnitOfWork _uow;
    private readonly IPriceCalculationService _priceService;

    public CustomizationService(IUnitOfWork uow, IPriceCalculationService priceService)
    {
        _uow = uow;
        _priceService = priceService;
    }

    public async Task<CustomizationDto?> GetByIdAsync(int id)
    {
        var c = await _uow.Customizations.GetWithDetailsAsync(id);
        return c == null ? null : MapDto(c);
    }

    public async Task<int> CreateCustomizationAsync(CreateCustomizationDto dto)
    {
        var costs = await _priceService.GetCustomizationCostsAsync(dto);
        var fee = costs.EmbroideryCost + costs.LogoCost;

        var customization = new Customization
        {
            WhiteCoatVariantId = (dto.WhiteCoatVariantId.HasValue && dto.WhiteCoatVariantId.Value > 0) 
                ? (int)dto.WhiteCoatVariantId.Value 
                : (int?)null,
            EmbroideryEnabled = dto.EmbroideryEnabled,
            StudentName = dto.StudentName,
            DoctorName = dto.DoctorName,
            Faculty = dto.Faculty,
            Department = dto.Department,
            GraduationYear = dto.GraduationYear,
            EmbroideryTextColor = dto.EmbroideryTextColor,
            EmbroideryFontId = dto.EmbroideryFontId,
            EmbroideryPosition = string.IsNullOrEmpty(dto.EmbroideryPosition)
                ? null
                : Enum.Parse<EmbroideryPosition>(dto.EmbroideryPosition),
            LogoType = string.IsNullOrEmpty(dto.LogoType) ? null : Enum.Parse<LogoSourceType>(dto.LogoType),
            LogoId = dto.LogoId,
            CustomLogoUrl = dto.CustomLogoUrl,
            CoatType = dto.CoatType,
            Size = dto.Size,
            Fabric = dto.Fabric,
            Color = dto.Color,
            PriceAdjustment = fee,
            EmbroideryCost = costs.EmbroideryCost,
            LogoCost = costs.LogoCost
        };

        await _uow.Customizations.AddAsync(customization);
        await _uow.SaveChangesAsync();

        // Persist shipping customization
        var shippingCustomization = new ShippingCustomization
        {
            CustomizationId = customization.Id,
            RequiresSpecialPackaging = dto.RequiresSpecialPackaging,
            IsFragile = dto.IsFragile
        };
        await _uow.ShippingCustomizations.AddAsync(shippingCustomization);
        await _uow.SaveChangesAsync();

        return customization.Id;
    }

    public async Task DeleteCustomizationAsync(int id)
    {
        var customization = await _uow.Customizations.GetByIdAsync(id);
        if (customization == null) return;
        customization.IsDeleted = true;
        customization.DeletedAt = DateTime.UtcNow;
        _uow.Customizations.Update(customization);
        await _uow.SaveChangesAsync();
    }

    private static CustomizationDto MapDto(Customization c) => new()
    {
        Id = c.Id,
        WhiteCoatVariantId = c.WhiteCoatVariantId,
        EmbroideryEnabled = c.EmbroideryEnabled,
        StudentName = c.StudentName,
        DoctorName = c.DoctorName,
        Faculty = c.Faculty,
        Department = c.Department,
        GraduationYear = c.GraduationYear,
        EmbroideryTextColor = c.EmbroideryTextColor,
        EmbroideryFontId = c.EmbroideryFontId,
        EmbroideryFontName = c.EmbroideryFont?.Name,
        EmbroideryPosition = c.EmbroideryPosition?.ToString(),
        LogoType = c.LogoType?.ToString(),
        LogoId = c.LogoId,
        LogoImageUrl = c.Logo?.ImageUrl,
        CustomLogoUrl = c.CustomLogoUrl,
        PreviewImageUrl = c.PreviewImageUrl,
        PriceAdjustment = c.PriceAdjustment,
        EmbroideryCost = c.EmbroideryCost,
        LogoCost = c.LogoCost
    };
}

/// <summary>
/// Infrastructure-only placeholder for IEmailService: no SMTP/email provider is configured yet,
/// so this logs what would have been sent instead of silently doing nothing. Swap this out for a
/// real provider (SendGrid, SMTP, etc.) by registering a different IEmailService implementation
/// in DependencyInjection.cs — the rest of the app already calls this interface everywhere it needs to.
/// </summary>
public class LoggingEmailService : IEmailService
{
    private readonly ILogger<LoggingEmailService> _logger;
    public LoggingEmailService(ILogger<LoggingEmailService> logger) => _logger = logger;

    public Task SendOrderConfirmationAsync(string email, OrderDto order)
    {
        _logger.LogInformation(
            "[Email placeholder] Order confirmation for {OrderNumber} would be sent to {Email} (total {Total:C}).",
            order.OrderNumber, email, order.Total);
        return Task.CompletedTask;
    }

    public Task SendOrderStatusUpdateAsync(string email, OrderDto order)
    {
        _logger.LogInformation(
            "[Email placeholder] Status update for {OrderNumber} ({Status}) would be sent to {Email}.",
            order.OrderNumber, order.Status, email);
        return Task.CompletedTask;
    }

    public Task SendWelcomeEmailAsync(string email, string name)
    {
        _logger.LogInformation("[Email placeholder] Welcome email for {Name} would be sent to {Email}.", name, email);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(string email, string resetLink)
    {
        _logger.LogInformation("[Email placeholder] Password reset link would be sent to {Email}.", email);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Focused stock reserve/release/check operations for White Coat variants, independent of the
/// checkout transaction in <c>OrderService.CreateOrderAsync</c>.
/// </summary>
public class WhiteCoatInventoryService : IWhiteCoatInventoryService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<WhiteCoatInventoryService> _logger;

    public WhiteCoatInventoryService(IUnitOfWork uow, ILogger<WhiteCoatInventoryService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<bool> IsInStockAsync(int variantId, int requestedQuantity)
    {
        var variant = await _uow.WhiteCoatVariants.GetByIdAsync(variantId);
        return variant != null && variant.IsActive && variant.StockQuantity >= requestedQuantity;
    }

    public async Task ReserveStockAsync(int variantId, int quantity)
    {
        var variant = await _uow.WhiteCoatVariants.GetByIdAsync(variantId)
            ?? throw new KeyNotFoundException($"White Coat variant {variantId} not found.");

        if (variant.StockQuantity < quantity)
            throw new InvalidOperationException($"Insufficient stock for variant {variant.SKU}.");

        variant.StockQuantity -= quantity;
        _uow.WhiteCoatVariants.Update(variant);
        await _uow.SaveChangesAsync();

        if (variant.StockQuantity <= variant.LowStockThreshold)
            _logger.LogWarning("White Coat variant {Sku} is now low on stock ({Qty} left).", variant.SKU, variant.StockQuantity);
    }

    public async Task ReleaseStockAsync(int variantId, int quantity)
    {
        var variant = await _uow.WhiteCoatVariants.GetByIdAsync(variantId);
        if (variant == null) return;

        variant.StockQuantity += quantity;
        _uow.WhiteCoatVariants.Update(variant);
        await _uow.SaveChangesAsync();
    }
}

/// <summary>
/// Determines extra shipping handling/fees for customized White Coat orders (embroidery/logo
/// work adds packaging and handling time) and maintains the audit trail of customization edits.
/// </summary>
public class ShippingCustomizationService : IShippingCustomizationService
{
    // Flat rules: any embroidery or custom logo adds packaging care + a short lead time.
    private const decimal SpecialPackagingFee = 2.50m;
    private const int EmbroideryExtraDays = 1;
    private const int CustomLogoExtraDays = 1;

    private readonly IUnitOfWork _uow;

    public ShippingCustomizationService(IUnitOfWork uow) => _uow = uow;

    public async Task<ShippingCustomizationDto?> GetByCustomizationIdAsync(int customizationId)
    {
        var s = await _uow.ShippingCustomizations.GetByCustomizationIdAsync(customizationId);
        return s == null ? null : MapDto(s);
    }

    public async Task<ShippingCustomizationDto> SetAsync(CreateShippingCustomizationDto dto)
    {
        var existing = await _uow.ShippingCustomizations.GetByCustomizationIdAsync(dto.CustomizationId);

        if (existing == null)
        {
            var created = new ShippingCustomization
            {
                CustomizationId = dto.CustomizationId,
                RequiresSpecialPackaging = dto.RequiresSpecialPackaging,
                IsFragile = dto.IsFragile,
                HandlingInstructions = dto.HandlingInstructions,
                EstimatedExtraDays = dto.EstimatedExtraDays,
                ExtraShippingFee = dto.ExtraShippingFee
            };
            await _uow.ShippingCustomizations.AddAsync(created);
            await _uow.SaveChangesAsync();
            return MapDto(created);
        }

        existing.RequiresSpecialPackaging = dto.RequiresSpecialPackaging;
        existing.IsFragile = dto.IsFragile;
        existing.HandlingInstructions = dto.HandlingInstructions;
        existing.EstimatedExtraDays = dto.EstimatedExtraDays;
        existing.ExtraShippingFee = dto.ExtraShippingFee;
        _uow.ShippingCustomizations.Update(existing);
        await _uow.SaveChangesAsync();
        return MapDto(existing);
    }

    public async Task<decimal> CalculateExtraFeeAsync(int customizationId)
    {
        var customization = await _uow.Customizations.GetWithDetailsAsync(customizationId)
            ?? throw new KeyNotFoundException($"Customization {customizationId} not found.");

        // If an admin has set explicit shipping rules for this customization, honor them.
        var existing = await _uow.ShippingCustomizations.GetByCustomizationIdAsync(customizationId);
        if (existing != null) return existing.ExtraShippingFee;

        // Otherwise fall back to the flat rule set based on what was customized.
        decimal fee = 0m;
        if (customization.EmbroideryEnabled) fee += SpecialPackagingFee;
        if (customization.LogoType == LogoSourceType.Custom) fee += SpecialPackagingFee;
        return fee;
    }

    public async Task<IEnumerable<CustomizationHistoryDto>> GetHistoryAsync(int customizationId)
    {
        var history = await _uow.CustomizationHistory.GetForCustomizationAsync(customizationId);
        return history.Select(h => new CustomizationHistoryDto
        {
            Id = h.Id,
            ChangedField = h.ChangedField,
            OldValue = h.OldValue,
            NewValue = h.NewValue,
            ChangedByUserId = h.ChangedByUserId,
            ChangedAt = h.ChangedAt
        });
    }

    private static ShippingCustomizationDto MapDto(ShippingCustomization s) => new()
    {
        Id = s.Id,
        CustomizationId = s.CustomizationId,
        RequiresSpecialPackaging = s.RequiresSpecialPackaging,
        IsFragile = s.IsFragile,
        HandlingInstructions = s.HandlingInstructions,
        EstimatedExtraDays = s.EstimatedExtraDays,
        ExtraShippingFee = s.ExtraShippingFee
    };
}

/// <summary>
/// Extends the existing CartService (see ServiceImplementations.cs) with the White Coat
/// add-to-cart flow: validates variant stock, prices the customization, persists it, and
/// attaches both to a new CartItem.
/// </summary>
public partial class CartService
{
    public async Task<CartDto> AddCustomizedItemToCartAsync(string? userId, string? sessionId, AddCustomizedItemToCartDto dto)
    {
        // Variant is now optional.
        WhiteCoatVariant? variant = null;
        if (dto.Customization.WhiteCoatVariantId.HasValue && dto.Customization.WhiteCoatVariantId.Value > 0)
        {
            variant = await _uow.WhiteCoatVariants.GetByIdAsync(dto.Customization.WhiteCoatVariantId.Value)
                ?? throw new KeyNotFoundException($"White Coat variant {dto.Customization.WhiteCoatVariantId.Value} not found.");

            if (!variant.IsActive || variant.StockQuantity < dto.Quantity)
                throw new InvalidOperationException($"White Coat variant {variant.SKU} does not have enough stock available.");
        }
        else
        {
            // Note: If no variant is chosen, we are skipping inventory stock checks for the variant.
            // If tracking inventory for the base product is desired, it should be implemented here.
        }

        var cart = await GetOrCreateCartAsync(userId, sessionId);
        var cartWithItems = (await _uow.Carts.GetWithItemsAsync(cart.Id))!;

        // Deduplication logic: Check for existing item with same Product, Variant, and Customization
        var existingItem = cartWithItems.Items.FirstOrDefault(i => 
            i.ProductId == dto.ProductId && 
            i.WhiteCoatVariantId == (dto.Customization.WhiteCoatVariantId ?? 0) &&
            i.Customization != null &&
            i.Customization.StudentName == dto.Customization.StudentName &&
            i.Customization.DoctorName == dto.Customization.DoctorName &&
            i.Customization.Faculty == dto.Customization.Faculty &&
            i.Customization.Department == dto.Customization.Department &&
            i.Customization.GraduationYear == dto.Customization.GraduationYear &&
            i.Customization.EmbroideryFontId == dto.Customization.EmbroideryFontId &&
            i.Customization.EmbroideryTextColor == dto.Customization.EmbroideryTextColor &&
            i.Customization.EmbroideryPosition == (string.IsNullOrEmpty(dto.Customization.EmbroideryPosition) ? null : Enum.Parse<EmbroideryPosition>(dto.Customization.EmbroideryPosition)) &&
            i.Customization.LogoType == (string.IsNullOrEmpty(dto.Customization.LogoType) ? null : Enum.Parse<LogoSourceType>(dto.Customization.LogoType)) &&
            i.Customization.LogoId == dto.Customization.LogoId &&
            i.Customization.CustomLogoUrl == dto.Customization.CustomLogoUrl
        );

        if (existingItem != null)
        {
            existingItem.Quantity += dto.Quantity;
            await _uow.SaveChangesAsync();
            return MapToDto(cartWithItems);
        }

        var costs = await _priceService.GetCustomizationCostsAsync(dto.Customization);
        var fee = costs.EmbroideryCost + costs.LogoCost;

        var customization = new Customization
        {
            WhiteCoatVariantId = (dto.Customization.WhiteCoatVariantId.HasValue && dto.Customization.WhiteCoatVariantId.Value > 0) 
                ? (int?)dto.Customization.WhiteCoatVariantId.Value 
                : null,

            EmbroideryEnabled = dto.Customization.EmbroideryEnabled,
            StudentName = dto.Customization.StudentName,
            DoctorName = dto.Customization.DoctorName,
            Faculty = dto.Customization.Faculty,
            Department = dto.Customization.Department,
            GraduationYear = dto.Customization.GraduationYear,
            EmbroideryTextColor = dto.Customization.EmbroideryTextColor,
            EmbroideryFontId = dto.Customization.EmbroideryFontId,
            EmbroideryPosition = string.IsNullOrEmpty(dto.Customization.EmbroideryPosition)
                ? null
                : Enum.Parse<EmbroideryPosition>(dto.Customization.EmbroideryPosition),
            LogoType = string.IsNullOrEmpty(dto.Customization.LogoType)
                ? null
                : Enum.Parse<LogoSourceType>(dto.Customization.LogoType),
            LogoId = dto.Customization.LogoId,
            CustomLogoUrl = dto.Customization.CustomLogoUrl,
            CoatType = dto.Customization.CoatType,
            Size = dto.Customization.Size,
            Fabric = dto.Customization.Fabric,
            Color = dto.Customization.Color,
            PriceAdjustment = fee,
            EmbroideryCost = costs.EmbroideryCost,
            LogoCost = costs.LogoCost
        };
        await _uow.Customizations.AddAsync(customization);
        await _uow.SaveChangesAsync(); // need the Customization.Id before attaching it to the cart item

        var cartItem = new CartItem
        {
            CartId = cart.Id,
            ProductId = dto.ProductId,
            Quantity = dto.Quantity,
            WhiteCoatVariantId = variant?.Id,
            CustomizationId = customization.Id
        };

        cartWithItems.Items.Add(cartItem);
        await _uow.SaveChangesAsync();

        var finalCart = (await _uow.Carts.GetWithItemsAsync(cart.Id))!;
        return MapToDto(finalCart);
    }
}
