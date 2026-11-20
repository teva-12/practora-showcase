using ShopElegance.Core.Enums;

namespace ShopElegance.Core.Entities;

public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? CompareAtPrice { get; set; }
    public decimal? DiscountPercentage { get; set; }
    public int StockQuantity { get; set; }
    public bool TrackInventory { get; set; } = true;

    /// <summary>Optimistic concurrency token so two simultaneous checkouts can't both oversell the last unit.</summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; }
    public ProductType Type { get; set; } = ProductType.Physical;
    public string? DownloadUrl { get; set; }
    public string? SKU { get; set; }
    public double? Weight { get; set; }
    public string? Dimensions { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public int ViewCount { get; set; }
    public int SalesCount { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    public ICollection<ProductTag> ProductTags { get; set; } = new List<ProductTag>();
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    public ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}
