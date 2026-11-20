using Microsoft.AspNetCore.Mvc;
using ShopElegance.Core.DTOs;
using ShopElegance.Core.Interfaces.Services;
using ShopElegance.Web.ViewModels;

namespace ShopElegance.Web.Controllers;

public class ShopController : Controller
{
    private readonly IProductService _productService;
    private readonly ICategoryService _categoryService;
    private readonly IWishlistService _wishlistService;
    private readonly IReviewService _reviewService;
    private readonly IWhiteCoatVariantService _whiteCoatVariantService;

    public ShopController(
        IProductService productService,
        ICategoryService categoryService,
        IWishlistService wishlistService,
        IReviewService reviewService,
        IWhiteCoatVariantService whiteCoatVariantService)
    {
        _productService = productService;
        _categoryService = categoryService;
        _wishlistService = wishlistService;
        _reviewService = reviewService;
        _whiteCoatVariantService = whiteCoatVariantService;
    }

    public async Task<IActionResult> Index(
        int page = 1, int pageSize = 12, int? categoryId = null,
        string? search = null, string? sortBy = "newest",
        decimal? minPrice = null, decimal? maxPrice = null,
        bool? inStock = null, string? productType = null)
    {
        var categories = await _categoryService.GetActiveCategoriesAsync();

        string? currentCategoryName = null;
        List<CategoryDto> subCategories = new List<CategoryDto>();

        if (categoryId.HasValue)
        {
            var cat = categories.FirstOrDefault(c => c.Id == categoryId.Value);
            currentCategoryName = cat?.Name;

            // 🎯 لو القسم ده رئيسي (زي الأسنان)، نجيب السنوات الفرعية التابعة له
            subCategories = categories
                .Where(c => c.ParentCategoryId == categoryId.Value)
                .OrderBy(c => c.SortOrder)
                .ToList();

            // 🎯 لو المستخدم اختار قسم رئيسي تحته أقسام فرعية ولم يقم بالبحث أو الفلترة
            // نقوم بتوجيهه لـ View الكروت (SubCategoriesGrid) لعرض السنوات الأربعة فقط
            if (subCategories.Any() && string.IsNullOrEmpty(search) && !minPrice.HasValue && !maxPrice.HasValue)
            {
                var parentCat = categories.FirstOrDefault(c => c.Id == categoryId.Value);
                ViewBag.SubCategories = subCategories;
                return View("SubCategoriesGrid", parentCat);
            }

            // 🎯 لو قسم فرعي أصلًا (زي سنة أولى)، نجيب أخواته التابعين لنفس الكلية
            if (!subCategories.Any() && cat?.ParentCategoryId != null)
            {
                subCategories = categories
                    .Where(c => c.ParentCategoryId == cat.ParentCategoryId)
                    .OrderBy(c => c.SortOrder)
                    .ToList();
            }
        }

        var filter = new ProductFilterDto
        {
            Page = page,
            PageSize = pageSize,
            CategoryId = categoryId,
            Search = search,
            SortBy = sortBy,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            InStock = inStock,
            ProductType = productType
        };

        var products = await _productService.GetProductsAsync(filter);

        // إرسال الأقسام الرئيسية فقط للـ Sidebar
        var mainCategories = categories.Where(c => c.ParentCategoryId == null).ToList();

        ViewBag.SubCategories = subCategories;

        var vm = new ShopViewModel
        {
            Products = products,
            Categories = mainCategories,
            Filter = filter,
            CurrentCategoryName = currentCategoryName
        };

        return View(vm);
    }

    public async Task<IActionResult> Product(string slug)
    {
        var product = await _productService.GetProductBySlugAsync(slug);
        if (product == null) return NotFound();

        await _productService.IncrementViewCountAsync(product.Id);

        var related = await _productService.GetRelatedProductsAsync(product.Id, product.CategoryId, 4);
        var reviews = await _reviewService.GetProductReviewsAsync(product.Id, 1, 5);

        bool isInWishlist = false;
        bool userHasReviewed = false;

        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId != null)
            {
                isInWishlist = await _wishlistService.IsInWishlistAsync(userId, product.Id);
            }
        }

        var vm = new ProductDetailViewModel
        {
            Product = product,
            RelatedProducts = related,
            Reviews = reviews,
            IsInWishlist = isInWishlist,
            UserHasReviewed = userHasReviewed,
            CanReview = !userHasReviewed,
            NewReview = new CreateReviewViewModel { ProductId = product.Id },
            HasWhiteCoatVariants = (await _whiteCoatVariantService.GetCustomerOptionsAsync(product.Id)).Variants.Any()
        };

        return View(vm);
    }

    [HttpPost]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> SubmitReview(CreateReviewViewModel model)
    {
        if (!ModelState.IsValid)
            return RedirectToAction("Product", new { slug = TempData["ProductSlug"] });

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!;
        await _reviewService.CreateReviewAsync(userId, new Core.DTOs.CreateReviewDto
        {
            ProductId = model.ProductId,
            Rating = model.Rating,
            Title = model.Title,
            Body = model.Body
        });

        TempData["SuccessMessage"] = "Thank you! Your review has been submitted and is pending approval.";
        var product = await _productService.GetProductByIdAsync(model.ProductId);
        return RedirectToAction("Product", new { slug = product?.Slug });
    }
}