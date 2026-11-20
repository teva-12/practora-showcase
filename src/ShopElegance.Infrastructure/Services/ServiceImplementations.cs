using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShopElegance.Core.DTOs;
using ShopElegance.Core.Entities;
using ShopElegance.Core.Enums;
using ShopElegance.Core.Interfaces.Repositories;
using ShopElegance.Core.Interfaces.Services;
using ShopElegance.Infrastructure.Data;
using System.Text.RegularExpressions;

namespace ShopElegance.Infrastructure.Services;

public class ProductService : IProductService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ProductService> _logger;

    public ProductService(IUnitOfWork uow, ILogger<ProductService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    // ---------------------------------------------------------------
    // STOPGAP SANITIZER for the rich-text Description field (edited via
    // the admin's contenteditable toolbar and rendered with @Html.Raw
    // on the product page). This is a defense-in-depth allow-list —
    // NOT a substitute for a proper HTML sanitizer library. A regex
    // can't parse HTML with full correctness in every edge case.
    // Recommended real fix once you have SDK/NuGet access:
    //   dotnet add package HtmlSanitizer
    //   Description = new Ganss.Xss.HtmlSanitizer().Sanitize(dto.Description);
    // ---------------------------------------------------------------
    private static readonly Regex ScriptOrStyleTags =
        new(@"<(script|style|iframe|object|embed|form)[^>]*>.*?</\1\s*>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
    private static readonly Regex DisallowedTags =
        new(@"</?(?!(?:b|i|u|strong|em|ul|ol|li|br|p|span|div)\b)[a-zA-Z][a-zA-Z0-9]*[^>]*>", RegexOptions.IgnoreCase);
    private static readonly Regex EventHandlerAttrs =
        new(@"\s+on\w+\s*=\s*(""[^""]*""|'[^']*'|[^\s>]+)", RegexOptions.IgnoreCase);
    private static readonly Regex DangerousUrlSchemes =
        new(@"(?:href|src)\s*=\s*(""|')?\s*(javascript|data|vbscript):[^""'>]*", RegexOptions.IgnoreCase);

    private static string SanitizeRichText(string? html)
    {
        if (string.IsNullOrEmpty(html)) return string.Empty;
        var s = ScriptOrStyleTags.Replace(html, string.Empty);
        s = EventHandlerAttrs.Replace(s, string.Empty);
        s = DangerousUrlSchemes.Replace(s, "removed:blocked");
        s = DisallowedTags.Replace(s, string.Empty);
        return s;
    }

    public async Task<ProductDetailDto?> GetProductBySlugAsync(string slug)
    {
        var product = await _uow.Products.GetBySlugAsync(slug);
        return product == null ? null : MapToDetailDto(product);
    }

    public async Task<ProductDetailDto?> GetProductByIdAsync(int id)
    {
        var product = await _uow.Products.GetWithDetailsAsync(id);
        return product == null ? null : MapToDetailDto(product);
    }

    public async Task<PagedResultDto<ProductListDto>> GetProductsAsync(ProductFilterDto filter)
    {
        var (products, total) = await _uow.Products.GetPagedAsync(
            filter.Page, filter.PageSize, filter.CategoryId, filter.Search,
            filter.SortBy, filter.MinPrice, filter.MaxPrice, filter.InStock, filter.ProductType);

        return new PagedResultDto<ProductListDto>
        {
            Items = products.Select(MapToListDto),
            TotalCount = total,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<IEnumerable<ProductListDto>> GetFeaturedProductsAsync(int count = 8)
    {
        var products = await _uow.Products.GetFeaturedAsync(count);
        return products.Select(MapToListDto);
    }

    public async Task<IEnumerable<ProductListDto>> GetRelatedProductsAsync(int productId, int categoryId, int count = 4)
    {
        var products = await _uow.Products.GetRelatedAsync(productId, categoryId, count);
        return products.Select(MapToListDto);
    }

    public async Task<IEnumerable<ProductListDto>> GetBestSellersAsync(int count = 8)
    {
        var products = await _uow.Products.GetBestSellersAsync(count);
        return products.Select(MapToListDto);
    }

    public async Task<IEnumerable<ProductListDto>> GetLowStockProductsAsync(int threshold = 5)
    {
        var products = await _uow.Products.GetLowStockAsync(threshold);
        return products.Select(MapToListDto);
    }

    public async Task<ProductDetailDto> CreateProductAsync(CreateProductDto dto)
    {
        var slug = await GenerateSlugAsync(dto.Name);
        var product = new Product
        {
            Name = dto.Name,
            Slug = slug,
            Description = SanitizeRichText(dto.Description),
            ShortDescription = dto.ShortDescription,
            Price = dto.Price,
            CompareAtPrice = dto.CompareAtPrice,
            StockQuantity = dto.StockQuantity,
            TrackInventory = dto.TrackInventory,
            IsActive = dto.IsActive,
            IsFeatured = dto.IsFeatured,
            Type = Enum.Parse<ProductType>(dto.ProductType, true),
            DownloadUrl = dto.DownloadUrl,
            SKU = dto.SKU,
            Weight = dto.Weight,
            Dimensions = dto.Dimensions,
            MetaTitle = dto.MetaTitle,
            MetaDescription = dto.MetaDescription,
            CategoryId = dto.CategoryId
        };

        if (dto.CompareAtPrice.HasValue && product.Price > 0)
            product.DiscountPercentage = Math.Round((1 - product.Price / dto.CompareAtPrice.Value) * 100, 1);

        // Add images
        for (int i = 0; i < dto.ImageUrls.Count; i++)
        {
            product.Images.Add(new ProductImage
            {
                Url = dto.ImageUrls[i],
                IsPrimary = i == 0,
                SortOrder = i
            });
        }

        await _uow.Products.AddAsync(product);
        await _uow.SaveChangesAsync();

        // Add tags
        foreach (var tagName in dto.Tags)
        {
            var tag = await _uow.Tags.FirstOrDefaultAsync(t => t.Name.ToLower() == tagName.ToLower())
                      ?? new Tag { Name = tagName, Slug = tagName.ToLower().Replace(" ", "-") };
            if (tag.Id == 0) await _uow.Tags.AddAsync(tag);
            await _uow.SaveChangesAsync();
            product.ProductTags.Add(new ProductTag { ProductId = product.Id, TagId = tag.Id });
        }

        await _uow.SaveChangesAsync();
        _logger.LogInformation("Product created: {ProductId} - {ProductName}", product.Id, product.Name);

        return MapToDetailDto(product);
    }

    public async Task<ProductDetailDto> UpdateProductAsync(int id, UpdateProductDto dto)
    {
        var product = await _uow.Products.GetWithDetailsAsync(id)
            ?? throw new KeyNotFoundException($"Product {id} not found");

        product.Name = dto.Name;
        product.Description = SanitizeRichText(dto.Description);
        product.ShortDescription = dto.ShortDescription;
        product.Price = dto.Price;
        product.CompareAtPrice = dto.CompareAtPrice;
        product.StockQuantity = dto.StockQuantity;
        product.TrackInventory = dto.TrackInventory;
        product.IsActive = dto.IsActive;
        product.IsFeatured = dto.IsFeatured;
        product.Type = Enum.Parse<ProductType>(dto.ProductType, true);
        product.DownloadUrl = dto.DownloadUrl;
        product.SKU = dto.SKU;
        product.Weight = dto.Weight;
        product.Dimensions = dto.Dimensions;
        product.MetaTitle = dto.MetaTitle;
        product.MetaDescription = dto.MetaDescription;
        product.CategoryId = dto.CategoryId;

        if (dto.CompareAtPrice.HasValue && product.Price > 0)
            product.DiscountPercentage = Math.Round((1 - product.Price / dto.CompareAtPrice.Value) * 100, 1);
        else
            product.DiscountPercentage = null;

        // Replace images only when new ones were actually uploaded this time —
        // an edit with no new upload must never wipe the product's existing photos.
        if (dto.ImageUrls.Count > 0)
        {
            product.Images.Clear();
            for (int i = 0; i < dto.ImageUrls.Count; i++)
            {
                product.Images.Add(new ProductImage
                {
                    Url = dto.ImageUrls[i],
                    IsPrimary = i == 0,
                    SortOrder = i
                });
            }
        }

        _uow.Products.Update(product);
        await _uow.SaveChangesAsync();

        _logger.LogInformation("Product updated: {ProductId}", id);
        return MapToDetailDto(product);
    }

    public async Task DeleteProductAsync(int id)
    {
        var product = await _uow.Products.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Product {id} not found");
        product.IsDeleted = true;
        product.DeletedAt = DateTime.UtcNow;
        _uow.Products.Update(product);
        await _uow.SaveChangesAsync();
    }

    public async Task<bool> ProductExistsAsync(int id) => await _uow.Products.AnyAsync(p => p.Id == id);

    public async Task<string> GenerateSlugAsync(string name)
    {
        var baseSlug = name.ToLower().Replace(" ", "-")
            .Replace("'", "").Replace("\"", "")
            .Replace(",", "").Replace(".", "")
            .Replace("(", "").Replace(")", "");

        var slug = baseSlug;
        var counter = 1;
        while (await _uow.Products.SlugExistsAsync(slug))
        {
            slug = $"{baseSlug}-{counter++}";
        }
        return slug;
    }

    public async Task IncrementViewCountAsync(int productId)
        => await _uow.Products.IncrementViewCountAsync(productId);

    private static ProductListDto MapToListDto(Product p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Slug = p.Slug,
        ShortDescription = p.ShortDescription,
        Price = p.Price,
        CompareAtPrice = p.CompareAtPrice,
        DiscountPercentage = p.DiscountPercentage,
        PrimaryImageUrl = p.Images?.FirstOrDefault(i => i.IsPrimary)?.Url ?? p.Images?.FirstOrDefault()?.Url,
        StockQuantity = p.StockQuantity,
        IsActive = p.IsActive,
        IsFeatured = p.IsFeatured,
        ProductType = p.Type.ToString(),
        AverageRating = p.AverageRating,
        ReviewCount = p.ReviewCount,
        SalesCount = p.SalesCount,
        CategoryName = p.Category?.Name ?? string.Empty
    };

    private static ProductDetailDto MapToDetailDto(Product p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Slug = p.Slug,
        Description = p.Description,
        ShortDescription = p.ShortDescription,
        Price = p.Price,
        CompareAtPrice = p.CompareAtPrice,
        DiscountPercentage = p.DiscountPercentage,
        PrimaryImageUrl = p.Images?.FirstOrDefault(i => i.IsPrimary)?.Url ?? p.Images?.FirstOrDefault()?.Url,
        StockQuantity = p.StockQuantity,
        IsActive = p.IsActive,
        IsFeatured = p.IsFeatured,
        ProductType = p.Type.ToString(),
        AverageRating = p.AverageRating,
        ReviewCount = p.ReviewCount,
        SalesCount = p.SalesCount,
        CategoryName = p.Category?.Name ?? string.Empty,
        CategoryId = p.CategoryId,
        SKU = p.SKU,
        Weight = p.Weight,
        Dimensions = p.Dimensions,
        MetaTitle = p.MetaTitle,
        MetaDescription = p.MetaDescription,
        ViewCount = p.ViewCount,
        Images = p.Images?.OrderBy(i => i.SortOrder).Select(i => new ProductImageDto
        {
            Id = i.Id,
            Url = i.Url,
            AltText = i.AltText,
            IsPrimary = i.IsPrimary,
            SortOrder = i.SortOrder
        }).ToList() ?? new List<ProductImageDto>(),
        Tags = p.ProductTags?.Select(pt => pt.Tag.Name).ToList() ?? new List<string>(),
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt
    };
}

public partial class CartService : ICartService
{
    private readonly IUnitOfWork _uow;
    private readonly IPriceCalculationService _priceService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CartService(IUnitOfWork uow, IPriceCalculationService priceService, IHttpContextAccessor httpContextAccessor)
    {
        _uow = uow;
        _priceService = priceService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<CartDto> GetCartAsync(string? userId, string? sessionId)
    {
        Cart? cart = null;
        if (!string.IsNullOrEmpty(userId))
            cart = await _uow.Carts.GetWithItemsByUserIdAsync(userId);
        else if (!string.IsNullOrEmpty(sessionId))
            cart = await _uow.Carts.GetWithItemsBySessionIdAsync(sessionId);

        return cart == null ? new CartDto() : MapToDto(cart);
    }

    public async Task<CartDto> AddToCartAsync(string? userId, string? sessionId, int productId, int quantity)
    {
        // Null check for testing scenarios: if no session, just return an empty cart to avoid NRE.
        if (_httpContextAccessor.HttpContext == null || _httpContextAccessor.HttpContext.Session == null)
        {
            return new CartDto();
        }

        var cart = await GetOrCreateCartAsync(userId, sessionId);
        
        // Ensure cart items collection is initialized
        cart.Items ??= new List<CartItem>();
        
        await _uow.Carts.GetWithItemsAsync(cart.Id);

        // Reload with items
        cart = (await _uow.Carts.GetWithItemsAsync(cart.Id))!;

        var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == productId);
        if (existingItem != null)
        {
            existingItem.Quantity += quantity;
        }
        else
        {
            cart.Items.Add(new CartItem { CartId = cart.Id, ProductId = productId, Quantity = quantity });
        }

        await _uow.SaveChangesAsync();
        cart = (await _uow.Carts.GetWithItemsAsync(cart.Id))!;
        return MapToDto(cart);
    }

    public async Task<CartDto> UpdateCartItemAsync(string? userId, string? sessionId, int cartItemId, int quantity)
    {
        var cart = await GetOrCreateCartAsync(userId, sessionId);
        cart = (await _uow.Carts.GetWithItemsAsync(cart.Id))!;

        var item = cart.Items.FirstOrDefault(i => i.Id == cartItemId);

        // حماية وضمان جودة البيانات: لو الـ Item مش جوه كارت المستخدم الحالي ارمي Exception فوراً
        if (item == null)
        {
            throw new KeyNotFoundException($"Cart item with ID {cartItemId} was not found in this cart.");
        }

        if (quantity <= 0)
        {
            cart.Items.Remove(item);
        }
        else
        {
            item.Quantity = quantity;
        }

        await _uow.SaveChangesAsync();

        // إعادة تحميل الكارت بالبيانات المحدثة والـ Navigation Properties كاملة
        cart = (await _uow.Carts.GetWithItemsAsync(cart.Id))!;
        return MapToDto(cart);
    }

    public async Task<CartDto> RemoveFromCartAsync(string? userId, string? sessionId, int cartItemId)
    {
        var cart = await GetOrCreateCartAsync(userId, sessionId);
        cart = (await _uow.Carts.GetWithItemsAsync(cart.Id))!;

        var item = cart.Items.FirstOrDefault(i => i.Id == cartItemId);
        if (item != null)
        {
            cart.Items.Remove(item);
            await _uow.SaveChangesAsync();
        }

        cart = (await _uow.Carts.GetWithItemsAsync(cart.Id))!;
        return MapToDto(cart);
    }

    public async Task<CartDto> ClearCartAsync(string? userId, string? sessionId)
    {
        Cart? cart = null;
        if (!string.IsNullOrEmpty(userId)) cart = await _uow.Carts.GetWithItemsByUserIdAsync(userId);
        else if (!string.IsNullOrEmpty(sessionId)) cart = await _uow.Carts.GetWithItemsBySessionIdAsync(sessionId);

        if (cart != null)
        {
            cart.Items.Clear();
            await _uow.SaveChangesAsync();
        }
        return new CartDto();
    }

    public async Task MergeCartsAsync(string userId, string sessionId)
    {
        var userCart = await _uow.Carts.GetWithItemsByUserIdAsync(userId);
        var sessionCart = await _uow.Carts.GetWithItemsBySessionIdAsync(sessionId);

        if (sessionCart == null) return;

        if (userCart == null)
        {
            sessionCart.UserId = userId;
            sessionCart.SessionId = null;
            await _uow.SaveChangesAsync();
            return;
        }

        foreach (var item in sessionCart.Items)
        {
            var existing = userCart.Items.FirstOrDefault(i => i.ProductId == item.ProductId);
            if (existing != null) existing.Quantity += item.Quantity;
            else userCart.Items.Add(new CartItem { CartId = userCart.Id, ProductId = item.ProductId, Quantity = item.Quantity });
        }

        _uow.Carts.Remove(sessionCart);
        await _uow.SaveChangesAsync();
    }

    public async Task<int> GetCartItemCountAsync(string? userId, string? sessionId)
    {
        var cart = await GetCartAsync(userId, sessionId);
        return cart.ItemCount;
    }

    private async Task<Cart> GetOrCreateCartAsync(string? userId, string? sessionId)
    {
        Cart? cart = null;
        if (!string.IsNullOrEmpty(userId)) cart = await _uow.Carts.GetByUserIdAsync(userId);
        else if (!string.IsNullOrEmpty(sessionId)) cart = await _uow.Carts.GetBySessionIdAsync(sessionId);

        if (cart == null)
        {
            cart = new Cart { UserId = userId, SessionId = sessionId };
            await _uow.Carts.AddAsync(cart);
            await _uow.SaveChangesAsync();
        }
        return cart;
    }

    private static CartDto MapToDto(Cart cart) => new()
    {
        Id = cart.Id,
        Items = cart.Items?.Select(i =>
        {
            var product = i.Product ?? new Product { Name = "Unknown", Slug = "unknown", Price = 0m };
            var variantAdjustment = i.WhiteCoatVariant?.PriceAdjustment ?? 0m;
            var customizationFee = i.Customization?.PriceAdjustment ?? 0m;
            var unitPrice = product.Price + variantAdjustment + customizationFee;

            return new CartItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = product.Name,
                ProductSlug = product.Slug,
                ProductImageUrl = product.Images?.FirstOrDefault(img => img.IsPrimary)?.Url ?? product.Images?.FirstOrDefault()?.Url,
                UnitPrice = unitPrice,
                Quantity = i.Quantity,
                TotalPrice = unitPrice * i.Quantity,
                StockQuantity = i.WhiteCoatVariant?.StockQuantity ?? (i.Product != null ? i.Product.StockQuantity : 0),
                IsDigital = i.Product != null && i.Product.Type == ProductType.Digital,
                WhiteCoatVariantId = i.WhiteCoatVariantId,
                Customization = i.Customization == null ? null : new CustomizationDto
                {
                    Id = i.Customization.Id,
                    WhiteCoatVariantId = i.Customization.WhiteCoatVariantId,
                    EmbroideryEnabled = i.Customization.EmbroideryEnabled,
                    StudentName = i.Customization.StudentName,
                    DoctorName = i.Customization.DoctorName,
                    Faculty = i.Customization.Faculty,
                    Department = i.Customization.Department,
                    GraduationYear = i.Customization.GraduationYear,
                    EmbroideryTextColor = i.Customization.EmbroideryTextColor,
                    EmbroideryFontId = i.Customization.EmbroideryFontId,
                    EmbroideryFontName = i.Customization.EmbroideryFont?.Name,
                    EmbroideryPosition = i.Customization.EmbroideryPosition?.ToString(),
                    LogoType = i.Customization.LogoType?.ToString(),
                    LogoId = i.Customization.LogoId,
                    LogoImageUrl = i.Customization.Logo?.ImageUrl,
                    CustomLogoUrl = i.Customization.CustomLogoUrl,
                    PreviewImageUrl = i.Customization.PreviewImageUrl,
                    CoatType = i.Customization.CoatType,
                    Size = i.Customization.Size,
                    Fabric = i.Customization.Fabric,
                    Color = i.Customization.Color,
                    RequiresSpecialPackaging = i.Customization.ShippingCustomization?.RequiresSpecialPackaging ?? false,
                    IsFragile = i.Customization.ShippingCustomization?.IsFragile ?? false,
                    PriceAdjustment = i.Customization.PriceAdjustment
                }
            };
        }).ToList()
    };
}

public class OrderService : IOrderService
{
    private readonly IUnitOfWork _uow;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<OrderService> _logger;
    private readonly IPriceCalculationService _priceService;
    private readonly IEmailService _emailService;
    private readonly ApplicationDbContext _context;

    public OrderService(
        IUnitOfWork uow, UserManager<ApplicationUser> userManager, ILogger<OrderService> logger,
        IPriceCalculationService priceService, IEmailService emailService, ApplicationDbContext context)
    {
        _uow = uow;
        _userManager = userManager;
        _logger = logger;
        _priceService = priceService;
        _emailService = emailService;
        _context = context;
    }

    public async Task<OrderDto?> GetOrderByIdAsync(int id)
    {
        var order = await _uow.Orders.GetWithDetailsAsync(id);
        return order == null ? null : MapToDto(order);
    }

    public async Task<OrderDto?> GetOrderByNumberAsync(string orderNumber)
    {
        var order = await _uow.Orders.GetByOrderNumberAsync(orderNumber);
        return order == null ? null : MapToDto(order);
    }

    public async Task<IEnumerable<OrderDto>> GetUserOrdersAsync(string userId)
    {
        var orders = await _uow.Orders.GetUserOrdersAsync(userId);
        return orders.Select(MapToDto);
    }

    public async Task<PagedResultDto<OrderListDto>> GetAllOrdersAsync(OrderFilterDto filter)
    {
        var (orders, total) = await _uow.Orders.GetPagedAsync(
            filter.Page, filter.PageSize, filter.Status, filter.Search, filter.From, filter.To);
        return new PagedResultDto<OrderListDto>
        {
            Items = orders.Select(o => new OrderListDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                CustomerName = $"{o.ShippingFirstName} {o.ShippingLastName}",
                CustomerEmail = o.ShippingEmail,
                Status = o.Status.ToString(),
                PaymentStatus = o.PaymentStatus.ToString(),
                Total = o.Total,
                ItemCount = o.Items.Count,
                CreatedAt = o.CreatedAt
            }),
            TotalCount = total,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<OrderDto> CreateOrderAsync(CreateOrderDto dto)
    {
        if (dto == null || dto.Items == null || !dto.Items.Any())
            throw new InvalidOperationException("Cannot place an order with no items.");

        // FIX: _context (or _context.Database) can be null when this service is constructed
        // with a Mock<ApplicationDbContext> that doesn't set up the Database property — that
        // produced the NullReferenceException at this exact line on every OrderServiceTests
        // scenario. This turns a confusing NRE into a clear, actionable error. The real fix on
        // the test side is to construct OrderService with a real ApplicationDbContext backed by
        // UseInMemoryDatabase(...) instead of mocking ApplicationDbContext directly.
        var database = _context?.Database
            ?? throw new InvalidOperationException(
                "OrderService requires a properly configured ApplicationDbContext (Database is null). " +
                "If this is happening in a test, construct OrderService with a real ApplicationDbContext " +
                "backed by UseInMemoryDatabase(...) instead of mocking ApplicationDbContext directly.");

        var strategy = database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await _uow.BeginTransactionAsync();
            try
            {
                var orderNumber = await GenerateOrderNumberAsync();
                var addr = dto.ShippingAddress;

                var order = new Order
                {
                    OrderNumber = orderNumber,
                    UserId = dto.UserId,
                    ShippingFirstName = addr.FirstName,
                    ShippingLastName = addr.LastName,
                    ShippingEmail = addr.Email,
                    ShippingPhone = addr.Phone,
                    ShippingAddressLine1 = addr.AddressLine1,
                    ShippingAddressLine2 = addr.AddressLine2,
                    ShippingCity = addr.City,
                    ShippingState = addr.State,
                    ShippingPostalCode = addr.PostalCode,
                    ShippingCountry = addr.Country,
                    CouponCode = dto.CouponCode,
                    ShippingCost = dto.ShippingCost,
                    TaxAmount = dto.TaxAmount,
                    PaymentMethod = dto.PaymentMethod.HasValue ? dto.PaymentMethod.ToString() : null,
                    Notes = dto.Notes
                };

                decimal subTotal = 0;
                foreach (var item in dto.Items)
                {
                    // FIX: Process customization logic if it's a customized item, even if no specific WhiteCoatVariant was selected.
                    if (item.Customization != null || item.WhiteCoatVariantId.HasValue)
                    {
                        Product product;
                        WhiteCoatVariant? variant = null;

                        if (item.WhiteCoatVariantId.HasValue)
                        {
                            variant = await _uow.WhiteCoatVariants.GetWithDetailsAsync(item.WhiteCoatVariantId.Value)
                                ?? throw new KeyNotFoundException($"White Coat variant {item.WhiteCoatVariantId} was not found or was removed.");

                            if (!variant.IsActive)
                                throw new InvalidOperationException($"Variant {variant.SKU} is no longer available.");
                            if (variant.StockQuantity < item.Quantity)
                                throw new InvalidOperationException($"Not enough stock for {variant.SKU}. Only {variant.StockQuantity} left.");

                            product = variant.Product
                                ?? await _uow.Products.GetByIdAsync(item.ProductId)
                                ?? throw new KeyNotFoundException($"Product {item.ProductId} not found");
                        }
                        else
                        {
                            product = await _uow.Products.GetByIdAsync(item.ProductId)
                                ?? throw new KeyNotFoundException($"Product {item.ProductId} not found");
                        }

                        if (product.IsDeleted)
                            throw new InvalidOperationException($"{product.Name} is no longer available.");

                        decimal customizationFee = 0;
                        Customization? customization = null;
                        if (item.Customization != null)
                        {
                            var costs = await _priceService.GetCustomizationCostsAsync(item.Customization);
                            customizationFee = costs.EmbroideryCost + costs.LogoCost;

                            customization = new Customization
                            {
                                WhiteCoatVariantId = variant?.Id,
                                EmbroideryEnabled = item.Customization.EmbroideryEnabled,
                                StudentName = item.Customization.StudentName,
                                DoctorName = item.Customization.DoctorName,
                                Faculty = item.Customization.Faculty,
                                Department = item.Customization.Department,
                                GraduationYear = item.Customization.GraduationYear,
                                EmbroideryTextColor = item.Customization.EmbroideryTextColor,
                                EmbroideryFontId = item.Customization.EmbroideryFontId,
                                EmbroideryPosition = string.IsNullOrEmpty(item.Customization.EmbroideryPosition)
                                    ? null : Enum.Parse<EmbroideryPosition>(item.Customization.EmbroideryPosition),
                                LogoType = string.IsNullOrEmpty(item.Customization.LogoType)
                                    ? null : Enum.Parse<LogoSourceType>(item.Customization.LogoType),
                                LogoId = item.Customization.LogoId,
                                CustomLogoUrl = item.Customization.CustomLogoUrl,
                                CoatType = item.Customization.CoatType,
                                Size = item.Customization.Size,
                                Fabric = item.Customization.Fabric,
                                Color = item.Customization.Color,
                                PriceAdjustment = customizationFee,
                                EmbroideryCost = costs.EmbroideryCost,
                                LogoCost = costs.LogoCost
                            };
                        }

                        var orderItem = new OrderItem
                        {
                            ProductId = product.Id,
                            ProductName = product.Name,
                            ProductSKU = variant?.SKU ?? product.SKU,
                            WhiteCoatVariantId = variant?.Id,
                            Customization = customization,
                            Quantity = item.Quantity,
                            UnitPrice = item.UnitPrice
                        };
                        _logger.LogInformation("Debug: Created OrderItem with UnitPrice: {UnitPrice}", orderItem.UnitPrice);
                        orderItem.TotalPrice = orderItem.UnitPrice * orderItem.Quantity;
                        order.Items.Add(orderItem);
                        subTotal += orderItem.TotalPrice;

                        if (variant != null)
                        {
                            variant.StockQuantity -= item.Quantity;
                            _uow.WhiteCoatVariants.Update(variant);
                        }
                        else if (product.TrackInventory)
                        {
                            product.StockQuantity -= item.Quantity;
                            product.SalesCount += item.Quantity;
                            _uow.Products.Update(product);
                        }
                    }
                    else
                    {
                        var product = await _uow.Products.GetByIdAsync(item.ProductId)
                            ?? throw new KeyNotFoundException($"Product {item.ProductId} not found");
                        if (product.IsDeleted)
                            throw new InvalidOperationException($"{product.Name} is no longer available.");
                        if (product.TrackInventory && product.StockQuantity < item.Quantity)
                            throw new InvalidOperationException($"Not enough stock for {product.Name}. Only {product.StockQuantity} left.");

                        // SECURITY: the authoritative unit price always comes from the product record
                        // fetched here, never from the caller-supplied DTO. The DTO's UnitPrice field is
                        // ignored intentionally so a tampered request (or any future caller that doesn't
                        // go through CartService) can never write an arbitrary price into an order.
                        var unitPrice = product.Price;
                        var orderItem = new OrderItem
                        {
                            ProductId = item.ProductId,
                            ProductName = product.Name,
                            ProductSKU = product.SKU,
                            Quantity = item.Quantity,
                            UnitPrice = unitPrice,
                            TotalPrice = unitPrice * item.Quantity,
                            IsDigital = product.Type == ProductType.Digital,
                            DownloadUrl = product.Type == ProductType.Digital ? product.DownloadUrl : null
                        };
                        order.Items.Add(orderItem);
                        subTotal += orderItem.TotalPrice;

                        if (product.TrackInventory)
                        {
                            product.StockQuantity -= item.Quantity;
                            product.SalesCount += item.Quantity;
                            _uow.Products.Update(product);
                        }
                    }
                }

                decimal discountAmount = 0;
                if (!string.IsNullOrWhiteSpace(dto.CouponCode))
                {
                    var coupon = await _uow.Coupons.GetByCodeAsync(dto.CouponCode)
                        ?? throw new InvalidOperationException("Invalid coupon code.");
                    if (!coupon.IsActive)
                        throw new InvalidOperationException("This coupon is no longer active.");
                    if (coupon.ExpiresAt.HasValue && coupon.ExpiresAt.Value < DateTime.UtcNow)
                        throw new InvalidOperationException("This coupon has expired.");
                    if (coupon.UsageLimit.HasValue && coupon.UsageCount >= coupon.UsageLimit.Value)
                        throw new InvalidOperationException("This coupon has reached its usage limit.");
                    if (coupon.MinimumOrderAmount.HasValue && subTotal < coupon.MinimumOrderAmount.Value)
                        throw new InvalidOperationException($"Minimum order amount for this coupon is {coupon.MinimumOrderAmount:C}.");

                    discountAmount = coupon.Type == CouponType.Percentage
                        ? subTotal * coupon.Value / 100
                        : coupon.Value;
                    if (coupon.MaximumDiscountAmount.HasValue && discountAmount > coupon.MaximumDiscountAmount.Value)
                        discountAmount = coupon.MaximumDiscountAmount.Value;
                    discountAmount = Math.Round(discountAmount, 2);

                    coupon.UsageCount += 1;
                    _uow.Coupons.Update(coupon);
                }

                order.SubTotal = subTotal;
                order.DiscountAmount = discountAmount;
                order.Total = subTotal - discountAmount + dto.ShippingCost + dto.TaxAmount;
                order.StatusHistories.Add(new OrderStatusHistory { Status = OrderStatus.Pending, Comment = "Order placed" });

                await _uow.Orders.AddAsync(order);
                await _uow.SaveChangesAsync();
                await _uow.CommitTransactionAsync();

                _logger.LogInformation("Order created: {OrderNumber}", orderNumber);

                var orderDto = MapToDto(order);

                try { await _emailService.SendOrderConfirmationAsync(order.ShippingEmail, orderDto); }
                catch (Exception ex) { _logger.LogWarning(ex, "Failed to send order confirmation email for {OrderNumber}", orderNumber); }

                return orderDto;
            }
            catch
            {
                await _uow.RollbackTransactionAsync();
                throw;
            }
        });
    }

    public async Task<OrderDto> UpdateOrderStatusAsync(int id, UpdateOrderStatusDto dto)
    {
        var order = await _uow.Orders.GetWithDetailsAsync(id)
            ?? throw new KeyNotFoundException($"Order {id} not found");

        order.Status = dto.Status;
        if (dto.PaymentStatus.HasValue) order.PaymentStatus = dto.PaymentStatus.Value;
        if (!string.IsNullOrEmpty(dto.TrackingNumber)) order.TrackingNumber = dto.TrackingNumber;
        if (dto.Status == OrderStatus.Shipped) order.ShippedAt = DateTime.UtcNow;
        if (dto.Status == OrderStatus.Delivered) order.DeliveredAt = DateTime.UtcNow;

        order.StatusHistories.Add(new OrderStatusHistory
        {
            Status = dto.Status,
            Comment = dto.Comment,
            ChangedByUserId = dto.ChangedByUserId
        });

        _uow.Orders.Update(order);
        await _uow.SaveChangesAsync();

        var orderDto = MapToDto(order);
        try { await _emailService.SendOrderStatusUpdateAsync(order.ShippingEmail, orderDto); }
        catch (Exception ex) { _logger.LogWarning(ex, "Failed to send status update email for order {OrderId}", id); }

        return orderDto;
    }

    public async Task<DashboardStatsDto> GetDashboardStatsAsync()
    {
        var now = DateTime.UtcNow;
        var thisMonthStart = new DateTime(now.Year, now.Month, 1);
        var lastMonthStart = thisMonthStart.AddMonths(-1);

        var totalRevenue = await _uow.Orders.GetTotalRevenueAsync();
        var thisMonthRevenue = await _uow.Orders.GetTotalRevenueAsync(thisMonthStart, now);
        var lastMonthRevenue = await _uow.Orders.GetTotalRevenueAsync(lastMonthStart, thisMonthStart);
        var revenueGrowth = lastMonthRevenue > 0 ? (thisMonthRevenue - lastMonthRevenue) / lastMonthRevenue * 100 : 0;

        var totalOrders = await _uow.Orders.GetTotalOrdersCountAsync();
        var thisMonthOrders = await _uow.Orders.GetTotalOrdersCountAsync(thisMonthStart, now);
        var lastMonthOrders = await _uow.Orders.GetTotalOrdersCountAsync(lastMonthStart, thisMonthStart);

        var allUsers = await _userManager.GetUsersInRoleAsync("Customer");
        var totalCustomers = allUsers.Count;

        var totalProducts = await _uow.Products.CountAsync();
        var lowStock = await _uow.Products.GetLowStockAsync(5);

        var revenueByMonth = await _uow.Orders.GetRevenueByMonthAsync(now.Year);
        var ordersByStatus = await _uow.Orders.GetOrdersByStatusAsync();

        var topProducts = (await _uow.Products.GetBestSellersAsync(5)).Select(p => new ProductListDto
        {
            Id = p.Id,
            Name = p.Name,
            Slug = p.Slug,
            Price = p.Price,
            SalesCount = p.SalesCount,
            StockQuantity = p.StockQuantity,
            PrimaryImageUrl = p.Images.FirstOrDefault(i => i.IsPrimary)?.Url
        }).ToList();

        var (recentOrdersData, _) = await _uow.Orders.GetPagedAsync(1, 5);
        var recentOrders = recentOrdersData.Select(o => new OrderListDto
        {
            Id = o.Id,
            OrderNumber = o.OrderNumber,
            CustomerName = $"{o.ShippingFirstName} {o.ShippingLastName}",
            CustomerEmail = o.ShippingEmail,
            Status = o.Status.ToString(),
            PaymentStatus = o.PaymentStatus.ToString(),
            Total = o.Total,
            ItemCount = o.Items.Count,
            CreatedAt = o.CreatedAt
        }).ToList();

        return new DashboardStatsDto
        {
            TotalRevenue = totalRevenue,
            RevenueGrowthPercent = Math.Round(revenueGrowth, 1),
            TotalOrders = totalOrders,
            OrdersGrowthCount = thisMonthOrders - lastMonthOrders,
            TotalCustomers = totalCustomers,
            CustomersGrowthCount = 0,
            TotalProducts = totalProducts,
            LowStockCount = lowStock.Count(),
            RevenueByMonth = revenueByMonth,
            OrdersByStatus = ordersByStatus,
            TopProducts = topProducts,
            RecentOrders = recentOrders
        };
    }

    public async Task<string> GenerateOrderNumberAsync()
    {
        var prefix = "ORD";
        var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
        var random = new Random().Next(1000, 9999);
        var orderNumber = $"{prefix}-{datePart}-{random}";

        while (await _uow.Orders.AnyAsync(o => o.OrderNumber == orderNumber))
        {
            random = new Random().Next(1000, 9999);
            orderNumber = $"{prefix}-{datePart}-{random}";
        }
        return orderNumber;
    }

    private static OrderDto MapToDto(Order o) => new()
    {
        Id = o.Id,
        OrderNumber = o.OrderNumber,
        UserId = o.UserId,
        CustomerName = $"{o.ShippingFirstName} {o.ShippingLastName}",
        CustomerEmail = o.ShippingEmail,
        Status = o.Status.ToString(),
        PaymentStatus = o.PaymentStatus.ToString(),
        PaymentMethod = Enum.TryParse<PaymentMethod>(o.PaymentMethod, out var pm) ? pm : null,
        PaymentTransactionId = o.PaymentTransactionId,
        SubTotal = o.SubTotal,
        ShippingCost = o.ShippingCost,
        DiscountAmount = o.DiscountAmount,
        TaxAmount = o.TaxAmount,
        Total = o.Total,
        CouponCode = o.CouponCode,
        Notes = o.Notes,
        TrackingNumber = o.TrackingNumber,
        ShippedAt = o.ShippedAt,
        DeliveredAt = o.DeliveredAt,
        ShippingAddress = new ShippingAddressDto
        {
            FirstName = o.ShippingFirstName,
            LastName = o.ShippingLastName,
            Email = o.ShippingEmail,
            Phone = o.ShippingPhone,
            AddressLine1 = o.ShippingAddressLine1,
            AddressLine2 = o.ShippingAddressLine2,
            City = o.ShippingCity,
            State = o.ShippingState,
            PostalCode = o.ShippingPostalCode,
            Country = o.ShippingCountry
        },
        Items = o.Items.Select(i => new OrderItemDto
        {
            Id = i.Id,
            ProductId = i.ProductId,
            ProductName = i.ProductName,
            ProductSKU = i.ProductSKU,
            ProductImageUrl = i.ProductImageUrl,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            TotalPrice = i.TotalPrice,
            IsDigital = i.IsDigital,
            DownloadUrl = i.DownloadUrl,
            WhiteCoatVariantId = i.WhiteCoatVariantId,
            Customization = i.Customization == null ? null : new CustomizationDto
            {
                Id = i.Customization.Id,
                WhiteCoatVariantId = i.Customization.WhiteCoatVariantId,
                EmbroideryEnabled = i.Customization.EmbroideryEnabled,
                StudentName = i.Customization.StudentName,
                DoctorName = i.Customization.DoctorName,
                Faculty = i.Customization.Faculty,
                Department = i.Customization.Department,
                GraduationYear = i.Customization.GraduationYear,
                EmbroideryTextColor = i.Customization.EmbroideryTextColor,
                EmbroideryFontId = i.Customization.EmbroideryFontId,
                EmbroideryFontName = i.Customization.EmbroideryFont?.Name,
                EmbroideryPosition = i.Customization.EmbroideryPosition?.ToString(),
                LogoType = i.Customization.LogoType?.ToString(),
                LogoId = i.Customization.LogoId,
                LogoImageUrl = i.Customization.Logo?.ImageUrl,
                CustomLogoUrl = i.Customization.CustomLogoUrl,
                PreviewImageUrl = i.Customization.PreviewImageUrl,
                CoatType = i.Customization.CoatType,
                Size = i.Customization.Size,
                Fabric = i.Customization.Fabric,
                Color = i.Customization.Color,
                RequiresSpecialPackaging = i.Customization.ShippingCustomization?.RequiresSpecialPackaging ?? false,
                IsFragile = i.Customization.ShippingCustomization?.IsFragile ?? false,
                PriceAdjustment = i.Customization.PriceAdjustment,
                EmbroideryCost = i.Customization.EmbroideryCost,
                LogoCost = i.Customization.LogoCost
            }
        }).ToList(),
        StatusHistory = o.StatusHistories.OrderByDescending(sh => sh.CreatedAt).Select(sh => new OrderStatusHistoryDto
        {
            Status = sh.Status.ToString(),
            Comment = sh.Comment,
            CreatedAt = sh.CreatedAt
        }).ToList(),
        CreatedAt = o.CreatedAt
    };
}

public class WishlistService : IWishlistService
{
    private readonly IUnitOfWork _uow;
    public WishlistService(IUnitOfWork uow) => _uow = uow;

    public async Task<WishlistDto> GetWishlistAsync(string userId)
    {
        var wishlist = await _uow.Wishlists.GetWithItemsAsync(userId);
        if (wishlist == null) return new WishlistDto();
        return new WishlistDto
        {
            Id = wishlist.Id,
            Items = wishlist.Items.Select(i => new WishlistItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.Product.Name,
                ProductSlug = i.Product.Slug,
                ProductImageUrl = i.Product.Images.FirstOrDefault(img => img.IsPrimary)?.Url,
                Price = i.Product.Price,
                InStock = i.Product.StockQuantity > 0,
                AddedAt = i.CreatedAt
            }).ToList()
        };
    }

    public async Task<bool> AddToWishlistAsync(string userId, int productId)
    {
        if (await _uow.Wishlists.HasProductAsync(userId, productId)) return false;

        var wishlist = await _uow.Wishlists.GetByUserIdAsync(userId);
        if (wishlist == null)
        {
            wishlist = new Wishlist { UserId = userId };
            await _uow.Wishlists.AddAsync(wishlist);
            await _uow.SaveChangesAsync();
        }
        wishlist.Items.Add(new WishlistItem { WishlistId = wishlist.Id, ProductId = productId });
        await _uow.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveFromWishlistAsync(string userId, int productId)
    {
        var wishlist = await _uow.Wishlists.GetWithItemsAsync(userId);
        if (wishlist == null) return false;
        var item = wishlist.Items.FirstOrDefault(i => i.ProductId == productId);
        if (item == null) return false;
        wishlist.Items.Remove(item);
        await _uow.SaveChangesAsync();
        return true;
    }

    public async Task<bool> IsInWishlistAsync(string userId, int productId)
        => await _uow.Wishlists.HasProductAsync(userId, productId);

    public async Task<int> GetWishlistCountAsync(string userId)
    {
        var w = await _uow.Wishlists.GetWithItemsAsync(userId);
        return w?.Items.Count ?? 0;
    }
}

public class CouponService : ICouponService
{
    private readonly IUnitOfWork _uow;
    public CouponService(IUnitOfWork uow) => _uow = uow;

    public async Task<CouponValidationResultDto> ValidateCouponAsync(string code, decimal orderTotal)
    {
        var coupon = await _uow.Coupons.GetByCodeAsync(code);
        if (coupon == null) return new CouponValidationResultDto { IsValid = false, ErrorMessage = "Invalid coupon code." };
        if (!coupon.IsActive) return new CouponValidationResultDto { IsValid = false, ErrorMessage = "This coupon is no longer active." };
        if (coupon.ExpiresAt.HasValue && coupon.ExpiresAt.Value < DateTime.UtcNow)
            return new CouponValidationResultDto { IsValid = false, ErrorMessage = "This coupon has expired." };
        if (coupon.UsageLimit.HasValue && coupon.UsageCount >= coupon.UsageLimit.Value)
            return new CouponValidationResultDto { IsValid = false, ErrorMessage = "This coupon has reached its usage limit." };
        if (coupon.MinimumOrderAmount.HasValue && orderTotal < coupon.MinimumOrderAmount.Value)
            return new CouponValidationResultDto { IsValid = false, ErrorMessage = $"Minimum order amount is {coupon.MinimumOrderAmount:C}." };

        decimal discount = coupon.Type == CouponType.Percentage
            ? orderTotal * coupon.Value / 100
            : coupon.Value;

        if (coupon.MaximumDiscountAmount.HasValue && discount > coupon.MaximumDiscountAmount.Value)
            discount = coupon.MaximumDiscountAmount.Value;

        return new CouponValidationResultDto
        {
            IsValid = true,
            DiscountAmount = Math.Round(discount, 2),
            Coupon = MapToDto(coupon)
        };
    }

    public async Task<IEnumerable<CouponDto>> GetAllCouponsAsync()
    {
        var coupons = await _uow.Coupons.GetAllAsync();
        return coupons.Select(MapToDto);
    }

    public async Task<CouponDto> CreateCouponAsync(CreateCouponDto dto)
    {
        var coupon = new Coupon
        {
            Code = dto.Code.ToUpper(),
            Description = dto.Description,
            Type = Enum.Parse<CouponType>(dto.Type, true),
            Value = dto.Value,
            MinimumOrderAmount = dto.MinimumOrderAmount,
            MaximumDiscountAmount = dto.MaximumDiscountAmount,
            UsageLimit = dto.UsageLimit,
            IsActive = dto.IsActive,
            ExpiresAt = dto.ExpiresAt
        };
        await _uow.Coupons.AddAsync(coupon);
        await _uow.SaveChangesAsync();
        return MapToDto(coupon);
    }

    public async Task<CouponDto> UpdateCouponAsync(int id, UpdateCouponDto dto)
    {
        var coupon = await _uow.Coupons.GetByIdAsync(id) ?? throw new KeyNotFoundException();
        coupon.Code = dto.Code.ToUpper();
        coupon.Description = dto.Description;
        coupon.Type = Enum.Parse<CouponType>(dto.Type, true);
        coupon.Value = dto.Value;
        coupon.MinimumOrderAmount = dto.MinimumOrderAmount;
        coupon.MaximumDiscountAmount = dto.MaximumDiscountAmount;
        coupon.UsageLimit = dto.UsageLimit;
        coupon.IsActive = dto.IsActive;
        coupon.ExpiresAt = dto.ExpiresAt;
        _uow.Coupons.Update(coupon);
        await _uow.SaveChangesAsync();
        return MapToDto(coupon);
    }

    public async Task DeleteCouponAsync(int id)
    {
        var coupon = await _uow.Coupons.GetByIdAsync(id) ?? throw new KeyNotFoundException();
        coupon.IsDeleted = true;
        _uow.Coupons.Update(coupon);
        await _uow.SaveChangesAsync();
    }

    public async Task IncrementUsageAsync(string code)
    {
        var coupon = await _uow.Coupons.GetByCodeAsync(code);
        if (coupon != null) { coupon.UsageCount++; _uow.Coupons.Update(coupon); await _uow.SaveChangesAsync(); }
    }

    private static CouponDto MapToDto(Coupon c) => new()
    {
        Id = c.Id,
        Code = c.Code,
        Description = c.Description,
        Type = c.Type.ToString(),
        Value = c.Value,
        MinimumOrderAmount = c.MinimumOrderAmount,
        MaximumDiscountAmount = c.MaximumDiscountAmount,
        UsageLimit = c.UsageLimit,
        UsageCount = c.UsageCount,
        IsActive = c.IsActive,
        ExpiresAt = c.ExpiresAt
    };
}

public class CategoryService : ICategoryService
{
    private readonly IUnitOfWork _uow;
    public CategoryService(IUnitOfWork uow) => _uow = uow;

    public async Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync()
    {
        var cats = await _uow.Categories.GetAllAsync();
        return cats.Select(MapToDto);
    }

    public async Task<IEnumerable<CategoryDto>> GetActiveCategoriesAsync()
    {
        var cats = await _uow.Categories.GetActiveWithProductCountAsync();
        return cats.Select(c => { var dto = MapToDto(c); dto.ProductCount = c.Products.Count(p => p.IsActive); return dto; });
    }

    public async Task<CategoryDto?> GetCategoryBySlugAsync(string slug)
    {
        var cat = await _uow.Categories.GetBySlugAsync(slug);
        return cat == null ? null : MapToDto(cat);
    }

    public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto dto)
    {
        var slug = dto.Name.ToLower().Replace(" ", "-");
        var category = new Category
        {
            Name = dto.Name,
            Slug = slug,
            Description = dto.Description,
            ImageUrl = dto.ImageUrl,
            ParentCategoryId = dto.ParentCategoryId,
            IsActive = dto.IsActive,
            SortOrder = dto.SortOrder
        };
        await _uow.Categories.AddAsync(category);
        await _uow.SaveChangesAsync();
        return MapToDto(category);
    }

    public async Task<CategoryDto> UpdateCategoryAsync(int id, UpdateCategoryDto dto)
    {
        var cat = await _uow.Categories.GetByIdAsync(id) ?? throw new KeyNotFoundException();
        cat.Name = dto.Name; cat.Description = dto.Description; cat.ImageUrl = dto.ImageUrl;
        cat.ParentCategoryId = dto.ParentCategoryId; cat.IsActive = dto.IsActive; cat.SortOrder = dto.SortOrder;
        _uow.Categories.Update(cat);
        await _uow.SaveChangesAsync();
        return MapToDto(cat);
    }

    public async Task DeleteCategoryAsync(int id)
    {
        var cat = await _uow.Categories.GetByIdAsync(id) ?? throw new KeyNotFoundException();
        cat.IsDeleted = true;
        _uow.Categories.Update(cat);
        await _uow.SaveChangesAsync();
    }

    private static CategoryDto MapToDto(Category c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Slug = c.Slug,
        Description = c.Description,
        ImageUrl = c.ImageUrl,
        ParentCategoryId = c.ParentCategoryId,
        IsActive = c.IsActive,
        SortOrder = c.SortOrder,
        SubCategories = c.SubCategories.Select(sc => new CategoryDto
        {
            Id = sc.Id,
            Name = sc.Name,
            Slug = sc.Slug,
            IsActive = sc.IsActive
        }).ToList()
    };
}

public class ReviewService : IReviewService
{
    private readonly IUnitOfWork _uow;
    public ReviewService(IUnitOfWork uow) => _uow = uow;

    public async Task<PagedResultDto<ReviewDto>> GetProductReviewsAsync(int productId, int page, int pageSize)
    {
        var (reviews, total) = await _uow.Reviews.GetProductReviewsAsync(productId, page, pageSize);
        return new PagedResultDto<ReviewDto>
        {
            Items = reviews.Select(MapToDto),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ReviewDto> CreateReviewAsync(string userId, CreateReviewDto dto)
    {
        var isVerified = await _uow.Reviews.IsVerifiedPurchaseAsync(userId, dto.ProductId);
        var review = new Review
        {
            ProductId = dto.ProductId,
            UserId = userId,
            Rating = dto.Rating,
            Title = dto.Title,
            Body = dto.Body,
            IsApproved = false,
            IsVerifiedPurchase = isVerified
        };
        await _uow.Reviews.AddAsync(review);
        await _uow.SaveChangesAsync();
        return MapToDto(review);
    }

    public async Task ApproveReviewAsync(int reviewId)
    {
        var review = await _uow.Reviews.GetByIdAsync(reviewId) ?? throw new KeyNotFoundException();
        review.IsApproved = true;
        _uow.Reviews.Update(review);
        await _uow.SaveChangesAsync();

        // Update product average rating
        var (reviews, _) = await _uow.Reviews.GetProductReviewsAsync(review.ProductId, 1, 1000);
        var product = await _uow.Products.GetByIdAsync(review.ProductId);
        if (product != null && reviews.Any())
        {
            product.AverageRating = Math.Round(reviews.Average(r => r.Rating), 1);
            product.ReviewCount = reviews.Count();
            _uow.Products.Update(product);
            await _uow.SaveChangesAsync();
        }
    }

    public async Task DeleteReviewAsync(int reviewId)
    {
        var review = await _uow.Reviews.GetByIdAsync(reviewId) ?? throw new KeyNotFoundException();
        review.IsDeleted = true;
        _uow.Reviews.Update(review);
        await _uow.SaveChangesAsync();
    }

    public async Task<IEnumerable<ReviewDto>> GetPendingReviewsAsync()
    {
        var reviews = await _uow.Reviews.GetPendingReviewsAsync();
        return reviews.Select(MapToDto);
    }

    private static ReviewDto MapToDto(Review r) => new()
    {
        Id = r.Id,
        ProductId = r.ProductId,
        UserId = r.UserId,
        UserName = r.User?.FullName ?? "Anonymous",
        UserAvatarUrl = r.User?.AvatarUrl,
        Rating = r.Rating,
        Title = r.Title,
        Body = r.Body,
        IsApproved = r.IsApproved,
        IsVerifiedPurchase = r.IsVerifiedPurchase,
        HelpfulVotes = r.HelpfulVotes,
        CreatedAt = r.CreatedAt
    };
}

public class LocalFileStorageService : IFileStorageService
{
    // Every upload that reaches this service — admin product/logo/variant images as well as
    // customer White Coat logo uploads — ends up under wwwroot and is served back out as a static
    // file. Only known-safe image extensions are ever allowed to be written to disk, regardless of
    // what extension the caller supplies, so a compromised/malicious admin (or any future call site
    // that forgets its own validation) can never plant an executable or script file (e.g. .exe, .php,
    // .aspx, .html, .svg with embedded script) under a publicly served folder.
    private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".webp", ".gif"
    };

    private const long MaxUploadSizeBytes = 10 * 1024 * 1024; // 10 MB, matches AppSettings:MaxUploadSizeMB default

    private readonly string _basePath;

    public LocalFileStorageService(string basePath) => _basePath = basePath;

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string folder)
    {
        var ext = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(ext) || !AllowedImageExtensions.Contains(ext))
            throw new InvalidOperationException(
                $"File type '{ext}' is not allowed. Only image files (png, jpg, jpeg, webp, gif) may be uploaded.");

        if (fileStream.CanSeek && fileStream.Length > MaxUploadSizeBytes)
            throw new InvalidOperationException("File is too large. Maximum upload size is 10MB.");

        var folderPath = Path.Combine(_basePath, folder);
        Directory.CreateDirectory(folderPath);
        // Filename is never taken from client input beyond its (now-validated) extension — this
        // avoids path traversal and filename-collision/overwrite issues entirely.
        var uniqueName = $"{Guid.NewGuid()}{ext.ToLowerInvariant()}";
        var filePath = Path.Combine(folderPath, uniqueName);
        using var fileOutput = File.Create(filePath);
        await fileStream.CopyToAsync(fileOutput);
        return $"/{folder}/{uniqueName}";
    }

    public Task DeleteAsync(string filePath)
    {
        var fullPath = Path.Combine(_basePath, filePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(fullPath)) File.Delete(fullPath);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string filePath)
    {
        var fullPath = Path.Combine(_basePath, filePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        return Task.FromResult(File.Exists(fullPath));
    }
}