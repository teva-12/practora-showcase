using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopElegance.Core.DTOs;
using ShopElegance.Core.Interfaces.Services;
using ShopElegance.Web.ViewModels;

namespace ShopElegance.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,Manager")]
public class ProductsController : Controller
{
    private readonly IProductService _productService;
    private readonly ICategoryService _categoryService;
    private readonly IFileStorageService _fileStorage;

    public ProductsController(
        IProductService productService,
        ICategoryService categoryService,
        IFileStorageService fileStorage)
    {
        _productService = productService;
        _categoryService = categoryService;
        _fileStorage = fileStorage;
    }

    public async Task<IActionResult> Index(int page = 1, string? search = null, int? categoryId = null)
    {
        var products = await _productService.GetProductsAsync(new ProductFilterDto
        {
            Page = page,
            PageSize = 20,
            Search = search,
            CategoryId = categoryId
        });
        ViewBag.Search = search;
        ViewBag.CategoryId = categoryId;

        // تجهيز الأقسام للفلترة في الـ Admin Index
        ViewBag.Categories = await GetFormattedCategoriesAsync();
        return View(products);
    }

    public async Task<IActionResult> Create()
    {
        var vm = new AdminProductViewModel
        {
            Categories = await GetFormattedCategoriesAsync()
        };
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Create(AdminProductViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Categories = await GetFormattedCategoriesAsync();
            return View(model);
        }

        var imageUrls = new List<string>();
        foreach (var image in model.Images.Where(f => f.Length > 0))
        {
            using var stream = image.OpenReadStream();
            var url = await _fileStorage.UploadAsync(stream, image.FileName, "images/products");
            imageUrls.Add(url);
        }

        var tags = model.TagsInput.Split(',', StringSplitOptions.RemoveEmptyEntries)
                       .Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t)).ToList();

        await _productService.CreateProductAsync(new CreateProductDto
        {
            Name = model.Name,
            Description = model.Description,
            ShortDescription = model.ShortDescription,
            Price = model.Price,
            CompareAtPrice = model.CompareAtPrice,
            StockQuantity = model.StockQuantity,
            TrackInventory = model.TrackInventory,
            IsActive = model.IsActive,
            IsFeatured = model.IsFeatured,
            ProductType = model.ProductType,
            DownloadUrl = model.DownloadUrl,
            SKU = model.SKU,
            Weight = model.Weight,
            Dimensions = model.Dimensions,
            MetaTitle = model.MetaTitle,
            MetaDescription = model.MetaDescription,
            CategoryId = model.CategoryId,
            Tags = tags,
            ImageUrls = imageUrls
        });

        TempData["SuccessMessage"] = "Product created successfully.";
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Edit(int id)
    {
        var product = await _productService.GetProductByIdAsync(id);
        if (product == null) return NotFound();

        var vm = new AdminProductViewModel
        {
            Name = product.Name,
            ShortDescription = product.ShortDescription,
            Description = product.Description,
            Price = product.Price,
            CompareAtPrice = product.CompareAtPrice,
            StockQuantity = product.StockQuantity,
            TrackInventory = true,
            IsActive = product.IsActive,
            IsFeatured = product.IsFeatured,
            ProductType = product.ProductType,
            SKU = product.SKU,
            Weight = product.Weight,
            Dimensions = product.Dimensions,
            MetaTitle = product.MetaTitle,
            MetaDescription = product.MetaDescription,
            CategoryId = product.CategoryId,
            TagsInput = string.Join(", ", product.Tags),
            Categories = await GetFormattedCategoriesAsync()
        };

        ViewBag.ProductId = id;
        ViewBag.ExistingImages = product.Images;
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, AdminProductViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Categories = await GetFormattedCategoriesAsync();
            ViewBag.ProductId = id;
            return View(model);
        }

        var tags = model.TagsInput.Split(',', StringSplitOptions.RemoveEmptyEntries)
                       .Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t)).ToList();

        var imageUrls = new List<string>();
        foreach (var image in model.Images.Where(f => f.Length > 0))
        {
            using var stream = image.OpenReadStream();
            var url = await _fileStorage.UploadAsync(stream, image.FileName, "images/products");
            imageUrls.Add(url);
        }

        await _productService.UpdateProductAsync(id, new UpdateProductDto
        {
            Id = id,
            Name = model.Name,
            Description = model.Description,
            ShortDescription = model.ShortDescription,
            Price = model.Price,
            CompareAtPrice = model.CompareAtPrice,
            StockQuantity = model.StockQuantity,
            TrackInventory = model.TrackInventory,
            IsActive = model.IsActive,
            IsFeatured = model.IsFeatured,
            ProductType = model.ProductType,
            DownloadUrl = model.DownloadUrl,
            SKU = model.SKU,
            Weight = model.Weight,
            Dimensions = model.Dimensions,
            MetaTitle = model.MetaTitle,
            MetaDescription = model.MetaDescription,
            CategoryId = model.CategoryId,
            Tags = tags,
            ImageUrls = imageUrls
        });

        TempData["SuccessMessage"] = "Product updated successfully.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        await _productService.DeleteProductAsync(id);
        TempData["SuccessMessage"] = "Product deleted.";
        return RedirectToAction("Index");
    }

    // 🎯 دالة مساعدة لتنسيق أسماء الأقسام في الـ Dropdown (مثلاً: Dentistry → 1st Year)
    private async Task<IEnumerable<CategoryDto>> GetFormattedCategoriesAsync()
    {
        var allCategories = (await _categoryService.GetAllCategoriesAsync()).ToList();

        foreach (var category in allCategories)
        {
            if (category.ParentCategoryId.HasValue)
            {
                var parent = allCategories.FirstOrDefault(p => p.Id == category.ParentCategoryId.Value);
                if (parent != null)
                {
                    category.Name = $"{parent.Name} ➔ {category.Name}";
                }
            }
        }

        return allCategories;
    }
}