using Microsoft.Extensions.Logging;
using ShopElegance.Core.DTOs;
using ShopElegance.Core.Interfaces.Services;
using ShopElegance.Infrastructure.Caching;

namespace ShopElegance.Infrastructure.Services;

/// <summary>
/// Transparent caching layer wrapping IProductService.
/// Decorates ProductService without any changes to the original implementation.
/// </summary>
public class CachedProductService : IProductService
{
    private readonly IProductService _inner;
    private readonly ICacheService _cache;
    private readonly ILogger<CachedProductService> _logger;

    private static readonly TimeSpan ShortTtl  = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan MediumTtl = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan LongTtl   = TimeSpan.FromMinutes(60);

    public CachedProductService(
        IProductService inner,
        ICacheService cache,
        ILogger<CachedProductService> logger)
    {
        _inner  = inner;
        _cache  = cache;
        _logger = logger;
    }

    public async Task<ProductDetailDto?> GetProductBySlugAsync(string slug)
    {
        var key = CacheKeys.ProductBySlug(slug);
        return await _cache.GetOrSetAsync(key,
            () => _inner.GetProductBySlugAsync(slug)!,
            MediumTtl);
    }

    public async Task<ProductDetailDto?> GetProductByIdAsync(int id)
    {
        var key = CacheKeys.ProductById(id);
        return await _cache.GetOrSetAsync(key,
            () => _inner.GetProductByIdAsync(id)!,
            MediumTtl);
    }

    // Paged results vary too much to cache globally; pass through
    public Task<PagedResultDto<ProductListDto>> GetProductsAsync(ProductFilterDto filter)
        => _inner.GetProductsAsync(filter);

    public async Task<IEnumerable<ProductListDto>> GetFeaturedProductsAsync(int count = 8)
    {
        var key = CacheKeys.FeaturedProducts(count);
        return await _cache.GetOrSetAsync(key,
            async () => (IEnumerable<ProductListDto>)await _inner.GetFeaturedProductsAsync(count),
            LongTtl);
    }

    public async Task<IEnumerable<ProductListDto>> GetBestSellersAsync(int count = 8)
    {
        var key = CacheKeys.BestSellers(count);
        return await _cache.GetOrSetAsync(key,
            async () => (IEnumerable<ProductListDto>)await _inner.GetBestSellersAsync(count),
            LongTtl);
    }

    public Task<IEnumerable<ProductListDto>> GetRelatedProductsAsync(int productId, int categoryId, int count = 4)
        => _inner.GetRelatedProductsAsync(productId, categoryId, count);

    public Task<IEnumerable<ProductListDto>> GetLowStockProductsAsync(int threshold = 5)
        => _inner.GetLowStockProductsAsync(threshold);

    public async Task<ProductDetailDto> CreateProductAsync(CreateProductDto dto)
    {
        var result = await _inner.CreateProductAsync(dto);
        await InvalidateProductCacheAsync();
        return result;
    }

    public async Task<ProductDetailDto> UpdateProductAsync(int id, UpdateProductDto dto)
    {
        var result = await _inner.UpdateProductAsync(id, dto);
        await _cache.RemoveAsync(CacheKeys.ProductById(id));
        await _cache.RemoveAsync(CacheKeys.ProductBySlug(result.Slug));
        await InvalidateProductCacheAsync();
        return result;
    }

    public async Task DeleteProductAsync(int id)
    {
        var product = await _inner.GetProductByIdAsync(id);
        await _inner.DeleteProductAsync(id);
        await _cache.RemoveAsync(CacheKeys.ProductById(id));
        if (product != null) await _cache.RemoveAsync(CacheKeys.ProductBySlug(product.Slug));
        await InvalidateProductCacheAsync();
    }

    public Task<bool> ProductExistsAsync(int id)
        => _inner.ProductExistsAsync(id);

    public Task<string> GenerateSlugAsync(string name)
        => _inner.GenerateSlugAsync(name);

    public Task IncrementViewCountAsync(int productId)
        => _inner.IncrementViewCountAsync(productId);

    private async Task InvalidateProductCacheAsync()
    {
        await _cache.RemoveByPrefixAsync(CacheKeys.ProductsPrefix);
        await _cache.RemoveAsync(CacheKeys.DashboardStats());
        _logger.LogDebug("Product cache invalidated");
    }
}
