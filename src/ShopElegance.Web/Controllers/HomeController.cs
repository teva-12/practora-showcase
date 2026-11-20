using Microsoft.AspNetCore.Mvc;
using ShopElegance.Core.DTOs;
using ShopElegance.Core.Interfaces.Services;

namespace ShopElegance.Web.Controllers;

public class HomeController : Controller
{
    private readonly IProductService _productService;
    private readonly ICategoryService _categoryService;

    public HomeController(IProductService productService, ICategoryService categoryService)
    {
        _productService = productService;
        _categoryService = categoryService;
    }

    public async Task<IActionResult> Index()
    {
        var featured = await _productService.GetFeaturedProductsAsync(8);
        var bestSellers = await _productService.GetBestSellersAsync(4);
        var allCategories = await _categoryService.GetActiveCategoriesAsync();

        // 🎯 الفلترة السحرية: جلب الكليات الرئيسية فقط (التي ليس لها ParentCategoryId)
        var mainFaculties = allCategories
            .Where(c => c.ParentCategoryId == null)
            .OrderBy(c => c.SortOrder)
            .ToList();

        ViewBag.FeaturedProducts = featured;
        ViewBag.BestSellers = bestSellers;
        ViewBag.Categories = mainFaculties; // نبعث الكليات الرئيسية فقط للـ View!

        // Resolve the real White Coat product
        var whiteCoatCategory = allCategories.FirstOrDefault(c =>
            c.Name.Trim().Equals("Design Your White Coat", StringComparison.OrdinalIgnoreCase));
        if (whiteCoatCategory != null)
        {
            var whiteCoatProducts = await _productService.GetProductsAsync(new ProductFilterDto
            {
                CategoryId = whiteCoatCategory.Id,
                PageSize = 1,
                SortBy = "newest"
            });
            ViewBag.WhiteCoatProductId = whiteCoatProducts.Items.FirstOrDefault()?.Id;
        }

        return View();
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        ViewBag.RequestId = HttpContext.TraceIdentifier;
        return View();
    }

    [HttpGet("Home/NotFound")]
    public IActionResult PageNotFound()
    {
        Response.StatusCode = 404;
        return View("NotFound");
    }
}