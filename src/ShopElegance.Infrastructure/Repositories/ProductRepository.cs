using Microsoft.EntityFrameworkCore;
using ShopElegance.Core.Entities;
using ShopElegance.Core.Interfaces.Repositories;
using ShopElegance.Infrastructure.Data;

namespace ShopElegance.Infrastructure.Repositories;

public class ProductRepository : GenericRepository<Product>, IProductRepository
{
    public ProductRepository(ApplicationDbContext context) : base(context) { }

    public async Task<Product?> GetBySlugAsync(string slug)
        => await _dbSet.Include(p => p.Category)
                       .Include(p => p.Images)
                       .Include(p => p.ProductTags).ThenInclude(pt => pt.Tag)
                       .FirstOrDefaultAsync(p => p.Slug == slug);

    public async Task<Product?> GetWithDetailsAsync(int id)
        => await _dbSet.Include(p => p.Category)
                       .Include(p => p.Images.OrderBy(i => i.SortOrder))
                       .Include(p => p.ProductTags).ThenInclude(pt => pt.Tag)
                       .Include(p => p.Reviews.Where(r => r.IsApproved))
                       .ThenInclude(r => r.User)
                       .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<(IEnumerable<Product> Products, int TotalCount)> GetPagedAsync(
        int page, int pageSize, int? categoryId = null,
        string? search = null, string? sortBy = null,
        decimal? minPrice = null, decimal? maxPrice = null,
        bool? inStock = null, string? productType = null)
    {
        var query = _dbSet.Include(p => p.Category)
                          .Include(p => p.Images.Where(i => i.IsPrimary))
                          .AsQueryable();

        if (categoryId.HasValue)
        {
            // جلب منتجات الكلية الرئيسية + منتجات الأقسام والسنوات الدراسية التابعة لها في استعلام واحد
            var categoryIdsQuery = _context.Categories
                .Where(c => c.Id == categoryId.Value || c.ParentCategoryId == categoryId.Value)
                .Select(c => c.Id);

            query = query.Where(p => categoryIdsQuery.Contains(p.CategoryId));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(term)
                                  || p.Description.ToLower().Contains(term)
                                  || p.ShortDescription.ToLower().Contains(term));
        }

        if (minPrice.HasValue) query = query.Where(p => p.Price >= minPrice.Value);
        if (maxPrice.HasValue) query = query.Where(p => p.Price <= maxPrice.Value);
        if (inStock.HasValue && inStock.Value) query = query.Where(p => p.StockQuantity > 0);
        if (!string.IsNullOrWhiteSpace(productType) && Enum.TryParse<Core.Enums.ProductType>(productType, out var pType))
            query = query.Where(p => p.Type == pType);

        var totalCount = await query.CountAsync();

        query = sortBy?.ToLower() switch
        {
            "price-asc" => query.OrderBy(p => p.Price),
            "price-desc" => query.OrderByDescending(p => p.Price),
            "name-asc" => query.OrderBy(p => p.Name),
            "bestselling" => query.OrderByDescending(p => p.SalesCount),
            "rating" => query.OrderByDescending(p => p.AverageRating),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };

        var products = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (products, totalCount);
    }

    public async Task<IEnumerable<Product>> GetFeaturedAsync(int count = 8)
        => await _dbSet.Where(p => p.IsFeatured && p.IsActive)
                       .Include(p => p.Images.Where(i => i.IsPrimary))
                       .OrderByDescending(p => p.SalesCount)
                       .Take(count)
                       .ToListAsync();

    public async Task<IEnumerable<Product>> GetRelatedAsync(int productId, int categoryId, int count = 4)
        => await _dbSet.Where(p => p.CategoryId == categoryId && p.Id != productId && p.IsActive)
                       .Include(p => p.Images.Where(i => i.IsPrimary))
                       .OrderByDescending(p => p.SalesCount)
                       .Take(count)
                       .ToListAsync();

    public async Task<IEnumerable<Product>> GetLowStockAsync(int threshold = 5)
        => await _dbSet.Where(p => p.TrackInventory && p.StockQuantity <= threshold && p.IsActive)
                       .Include(p => p.Images.Where(i => i.IsPrimary))
                       .OrderBy(p => p.StockQuantity)
                       .ToListAsync();

    public async Task<IEnumerable<Product>> GetBestSellersAsync(int count = 8)
        => await _dbSet.Where(p => p.IsActive)
                       .Include(p => p.Images.Where(i => i.IsPrimary))
                       .OrderByDescending(p => p.SalesCount)
                       .Take(count)
                       .ToListAsync();

    public async Task IncrementViewCountAsync(int productId)
    {
        if (_context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            var product = await _dbSet.FindAsync(productId);
            if (product != null)
            {
                product.ViewCount++;
                await _context.SaveChangesAsync();
            }
        }
        else
        {
            await _dbSet.Where(p => p.Id == productId)
                        .ExecuteUpdateAsync(s => s.SetProperty(p => p.ViewCount, p => p.ViewCount + 1));
        }
    }

    public async Task<bool> SlugExistsAsync(string slug, int? excludeId = null)
    {
        var query = _dbSet.Where(p => p.Slug == slug);
        if (excludeId.HasValue) query = query.Where(p => p.Id != excludeId.Value);
        return await query.AnyAsync();
    }
}