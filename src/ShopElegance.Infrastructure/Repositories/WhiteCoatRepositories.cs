using Microsoft.EntityFrameworkCore;
using ShopElegance.Core.Entities;
using ShopElegance.Core.Enums;
using ShopElegance.Core.Interfaces.Repositories;
using ShopElegance.Infrastructure.Data;

namespace ShopElegance.Infrastructure.Repositories;

public class WhiteCoatVariantRepository : GenericRepository<WhiteCoatVariant>, IWhiteCoatVariantRepository
{
    public WhiteCoatVariantRepository(ApplicationDbContext context) : base(context) { }

    public async Task<WhiteCoatVariant?> GetWithDetailsAsync(int id)
        => await _dbSet
            .Include(v => v.Product)
            .Include(v => v.Images.OrderBy(i => i.SortOrder))
            .FirstOrDefaultAsync(v => v.Id == id);

    public async Task<IEnumerable<WhiteCoatVariant>> GetByProductIdAsync(int productId, bool activeOnly = true)
    {
        var query = _dbSet.Include(v => v.Images).Where(v => v.ProductId == productId);
        if (activeOnly) query = query.Where(v => v.IsActive);
        return await query.ToListAsync();
    }

    public async Task<(IEnumerable<WhiteCoatVariant> Variants, int TotalCount)> GetPagedAsync(
        int page, int pageSize, int? productId = null, string? search = null,
        string? size = null, string? color = null, bool? isActive = null,
        bool? lowStockOnly = null, string? sortBy = null)
    {
        var query = _dbSet.Include(v => v.Product).Include(v => v.Images).AsQueryable();

        if (productId.HasValue) query = query.Where(v => v.ProductId == productId);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(v => v.SKU.Contains(search) || v.Color.Contains(search) || v.Fabric.Contains(search));
        if (!string.IsNullOrWhiteSpace(size))
            query = query.Where(v => v.Size == size);
        if (!string.IsNullOrWhiteSpace(color)) query = query.Where(v => v.Color == color);
        if (isActive.HasValue) query = query.Where(v => v.IsActive == isActive);
        if (lowStockOnly == true) query = query.Where(v => v.StockQuantity <= v.LowStockThreshold);

        query = sortBy switch
        {
            "price_asc" => query.OrderBy(v => v.PriceAdjustment),
            "price_desc" => query.OrderByDescending(v => v.PriceAdjustment),
            "stock_asc" => query.OrderBy(v => v.StockQuantity),
            _ => query.OrderByDescending(v => v.CreatedAt)
        };

        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, total);
    }

    public async Task<IEnumerable<WhiteCoatVariant>> GetLowStockAsync()
        => await _dbSet.Where(v => v.IsActive && v.StockQuantity <= v.LowStockThreshold).ToListAsync();

    public async Task<bool> SkuExistsAsync(string sku, int? excludeId = null)
        => await _dbSet.AnyAsync(v => v.SKU == sku && (!excludeId.HasValue || v.Id != excludeId.Value));

    public async Task AdjustStockAsync(int variantId, int delta)
    {
        var variant = await _dbSet.FindAsync(variantId);
        if (variant == null) return;
        variant.StockQuantity = Math.Max(0, variant.StockQuantity + delta);
    }

    public async Task<WhiteCoatVariant?> GetByImageIdAsync(int imageId)
        => await _dbSet.Include(v => v.Images)
                       .FirstOrDefaultAsync(v => v.Images.Any(i => i.Id == imageId));
}

public class EmbroideryFontRepository : GenericRepository<EmbroideryFont>, IEmbroideryFontRepository
{
    public EmbroideryFontRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<EmbroideryFont>> GetActiveAsync()
        => await _dbSet.Where(f => f.IsActive).ToListAsync();
}

public class LogoRepository : GenericRepository<Logo>, ILogoRepository
{
    public LogoRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<Logo>> GetActiveAsync(LogoSourceType? type = null)
    {
        var query = _dbSet.Where(l => l.IsActive);
        if (type.HasValue) query = query.Where(l => l.Type == type.Value);
        return await query.ToListAsync();
    }
}

public class CustomizationRepository : GenericRepository<Customization>, ICustomizationRepository
{
    public CustomizationRepository(ApplicationDbContext context) : base(context) { }

    public async Task<Customization?> GetWithDetailsAsync(int id)
        => await _dbSet
            .Include(c => c.WhiteCoatVariant)
            .Include(c => c.EmbroideryFont)
            .Include(c => c.Logo)
            .Include(c => c.ShippingCustomization)
            .FirstOrDefaultAsync(c => c.Id == id);
}

public class CustomizationHistoryRepository : GenericRepository<CustomizationHistory>, ICustomizationHistoryRepository
{
    public CustomizationHistoryRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<CustomizationHistory>> GetForCustomizationAsync(int customizationId)
        => await _dbSet
            .Where(h => h.CustomizationId == customizationId)
            .OrderByDescending(h => h.ChangedAt)
            .ToListAsync();

    public async Task LogChangeAsync(int customizationId, string changedField, string? oldValue, string? newValue, string? changedByUserId)
    {
        await _dbSet.AddAsync(new CustomizationHistory
        {
            CustomizationId = customizationId,
            ChangedField = changedField,
            OldValue = oldValue,
            NewValue = newValue,
            ChangedByUserId = changedByUserId,
            ChangedAt = DateTime.UtcNow
        });
    }
}

public class ShippingCustomizationRepository : GenericRepository<ShippingCustomization>, IShippingCustomizationRepository
{
    public ShippingCustomizationRepository(ApplicationDbContext context) : base(context) { }

    public async Task<ShippingCustomization?> GetByCustomizationIdAsync(int customizationId)
        => await _dbSet.FirstOrDefaultAsync(s => s.CustomizationId == customizationId);
}
