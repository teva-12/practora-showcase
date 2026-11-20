using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopElegance.Core.DTOs;
using ShopElegance.Core.Interfaces.Services;

namespace ShopElegance.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,Manager")]
public class WhiteCoatVariantsController : Controller
{
    private readonly IWhiteCoatVariantService _variantService;
    private readonly IProductService _productService;
    private readonly IFileStorageService _fileStorage;

    public WhiteCoatVariantsController(
        IWhiteCoatVariantService variantService,
        IProductService productService,
        IFileStorageService fileStorage)
    {
        _variantService = variantService;
        _productService = productService;
        _fileStorage = fileStorage;
    }

    public async Task<IActionResult> Index(
        int page = 1, string? search = null, int? productId = null,
        string? size = null, string? color = null, bool? isActive = null,
        bool? lowStockOnly = null, string? sortBy = null)
    {
        var result = await _variantService.GetVariantsAsync(new WhiteCoatVariantFilterDto
        {
            Page = page,
            PageSize = 20,
            Search = search,
            ProductId = productId,
            Size = size,
            Color = color,
            IsActive = isActive,
            LowStockOnly = lowStockOnly,
            SortBy = sortBy
        });

        ViewBag.Search = search;
        ViewBag.ProductId = productId;
        ViewBag.Size = size;
        ViewBag.Color = color;
        ViewBag.IsActive = isActive;
        ViewBag.LowStockOnly = lowStockOnly;
        ViewBag.SortBy = sortBy;
        ViewBag.Products = (await _productService.GetProductsAsync(new ProductFilterDto { Page = 1, PageSize = 200 })).Items;

        return View(result);
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.Products = (await _productService.GetProductsAsync(new ProductFilterDto { Page = 1, PageSize = 200 })).Items;
        return View(new CreateWhiteCoatVariantDto { StockQuantity = 0, LowStockThreshold = 5, IsActive = true });
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateWhiteCoatVariantDto dto)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Products = (await _productService.GetProductsAsync(new ProductFilterDto { Page = 1, PageSize = 200 })).Items;
            return View(dto);
        }

        try
        {
            var created = await _variantService.CreateVariantAsync(dto);
            TempData["SuccessMessage"] = "White Coat variant created successfully.";
            return RedirectToAction("Edit", new { id = created.Id });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("SKU", ex.Message);
            ViewBag.Products = (await _productService.GetProductsAsync(new ProductFilterDto { Page = 1, PageSize = 200 })).Items;
            return View(dto);
        }
    }

    public async Task<IActionResult> Edit(int id)
    {
        var variant = await _variantService.GetVariantByIdAsync(id);
        if (variant == null) return NotFound();

        ViewBag.Products = (await _productService.GetProductsAsync(new ProductFilterDto { Page = 1, PageSize = 200 })).Items;
        ViewBag.VariantId = id;
        ViewBag.Images = variant.Images;

        return View(new UpdateWhiteCoatVariantDto
        {
            ProductId = variant.ProductId,
            Size = variant.Size,
            Color = variant.Color,
            Fabric = variant.Fabric,
            CoatType = variant.CoatType,
            SKU = variant.SKU,
            Barcode = variant.Barcode,
            PriceAdjustment = variant.PriceAdjustment,
            Weight = variant.Weight,
            StockQuantity = variant.StockQuantity,
            LowStockThreshold = variant.LowStockThreshold,
            IsActive = variant.IsActive
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, UpdateWhiteCoatVariantDto dto)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Products = (await _productService.GetProductsAsync(new ProductFilterDto { Page = 1, PageSize = 200 })).Items;
            ViewBag.VariantId = id;
            ViewBag.Images = (await _variantService.GetVariantByIdAsync(id))?.Images ?? new List<WhiteCoatVariantImageDto>();
            return View(dto);
        }

        try
        {
            await _variantService.UpdateVariantAsync(id, dto);
            TempData["SuccessMessage"] = "White Coat variant updated.";
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("SKU", ex.Message);
            ViewBag.Products = (await _productService.GetProductsAsync(new ProductFilterDto { Page = 1, PageSize = 200 })).Items;
            ViewBag.VariantId = id;
            ViewBag.Images = (await _variantService.GetVariantByIdAsync(id))?.Images ?? new List<WhiteCoatVariantImageDto>();
            return View(dto);
        }

        return RedirectToAction("Edit", new { id });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        await _variantService.DeleteVariantAsync(id);
        TempData["SuccessMessage"] = "White Coat variant deleted.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> UploadImage(int variantId, IFormFile file, bool isPrimary = false)
    {
        if (file == null || file.Length == 0)
        {
            TempData["ErrorMessage"] = "Please choose an image file.";
            return RedirectToAction("Edit", new { id = variantId });
        }

        using var stream = file.OpenReadStream();
        var url = await _fileStorage.UploadAsync(stream, file.FileName, "images/whitecoats");
        await _variantService.AddImageAsync(variantId, url, file.FileName, isPrimary);

        TempData["SuccessMessage"] = "Image uploaded.";
        return RedirectToAction("Edit", new { id = variantId });
    }

    [HttpPost]
    public async Task<IActionResult> RemoveImage(int variantId, int imageId)
    {
        await _variantService.RemoveImageAsync(imageId);
        TempData["SuccessMessage"] = "Image removed.";
        return RedirectToAction("Edit", new { id = variantId });
    }
    [HttpPost]
    [ValidateAntiForgeryToken] // إضافة حماية الـ Token
    public async Task<IActionResult> BulkUpdate([FromForm] BulkUpdateWhiteCoatVariantDto dto)
    {
        // 1. التأكد من إن الـ DTO وصل ببيانات سليمة
        if (dto == null || dto.VariantIds == null || !dto.VariantIds.Any())
        {
            TempData["ErrorMessage"] = "رجاء اختيار عنصر واحد على الأقل للتعديل.";
            return RedirectToAction("Index");
        }

        if (!ModelState.IsValid)
        {
            // هيساعدك تعرف إيه الحقل اللي فيه مشكلة بالضبط
            var errors = string.Join(" | ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));

            TempData["ErrorMessage"] = $"بيانات غير صالحة: {errors}";
            return RedirectToAction("Index");
        }

        try
        {
            await _variantService.BulkUpdateAsync(dto);
            TempData["SuccessMessage"] = $"{dto.VariantIds.Count} variant(s) updated.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"حدث خطأ أثناء التحديث: {ex.Message}";
        }

        return RedirectToAction("Index");
    }
}
