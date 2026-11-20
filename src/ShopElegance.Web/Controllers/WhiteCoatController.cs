using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using ShopElegance.Core.DTOs;
using ShopElegance.Core.Interfaces.Services;
using ShopElegance.Web.ViewModels;

namespace ShopElegance.Web.Controllers;

public class WhiteCoatController : Controller
{
    private static readonly string[] AllowedLogoExtensions = { ".png", ".jpg", ".jpeg", ".webp" };
    private static readonly string[] AllowedLogoMimeTypes = { "image/png", "image/jpeg", "image/webp" };
    private const long MaxLogoFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    private readonly IWhiteCoatVariantService _variantService;
    private readonly ICustomizationPreviewService _previewService;
    private readonly ICartService _cartService;
    private readonly IProductService _productService;
    private readonly IFileStorageService _fileStorage;
    private readonly IValidator<AddCustomizedItemToCartDto> _addToCartValidator;

    public WhiteCoatController(
        IWhiteCoatVariantService variantService,
        ICustomizationPreviewService previewService,
        ICartService cartService,
        IProductService productService,
        IFileStorageService fileStorage,
        IValidator<AddCustomizedItemToCartDto> addToCartValidator)
    {
        _variantService = variantService;
        _previewService = previewService;
        _cartService = cartService;
        _productService = productService;
        _fileStorage = fileStorage;
        _addToCartValidator = addToCartValidator;
    }

    private string? UserId => User.Identity?.IsAuthenticated == true
        ? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        : null;

    private string SessionId 
    {
        get
        {
            var id = HttpContext.Session.Id;
            HttpContext.Session.SetString("CartSessionId", id); // forces session to persist/commit
            return id;
        }
    }

    /// <summary>Customer-facing customization page for a White Coat–eligible product.</summary>
    [HttpGet]
    public async Task<IActionResult> Customize(int productId)
    {
        var product = await _productService.GetProductByIdAsync(productId);
        if (product == null) return NotFound();

        // 1. تحديد مسار الصورة الأساسية
        string? imageUrl = product.PrimaryImageUrl;

        // 2. تجربة جلب أول صورة من القائمة لو الأساسية مش موجودة
        if (string.IsNullOrEmpty(imageUrl) && product.Images != null && product.Images.Any())
        {
            imageUrl = product.Images.FirstOrDefault()?.Url;
        }

        // 3. ضبط الـ Slash في بداية المسار
        if (!string.IsNullOrEmpty(imageUrl) && !imageUrl.StartsWith("/"))
        {
            imageUrl = "/" + imageUrl;
        }

        // 4. صورة افتراضية احتياطية
        if (string.IsNullOrEmpty(imageUrl))
        {
            imageUrl = "/images/default-coat.jpg";
        }

        // 5. جلب خيارات التخصيص للمنتج
        var options = await _variantService.GetCustomerOptionsAsync(productId);

        // 6. بناء الـ ViewModel وتمرير الصورة والمنتج والخيارات بأمان
        var viewModel = new WhiteCoatCustomizeViewModel
        {
            Product = product,
            Options = options
        };

        // تحديد القيمة الجاهزة جواه عشان الـ View يقرأها صح بالكامل
        product.PrimaryImageUrl = imageUrl;

        return View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> Preview([FromBody] WhiteCoatPreviewRequestDto request)
    {
        try
        {
            var result = await _previewService.GeneratePreviewAsync(request);

            string? baseImage = result.BaseImageUrl;

            // لو الـ Service مرجعتش صورة
            if (string.IsNullOrEmpty(baseImage))
            {
                // جرب حط اسم الـ ID الصح اللي جوه WhiteCoatPreviewRequestDto
                // لو مش عارف اسم الـ Property بالضبط، الكود ده هيمشي الأمور من غير ما يضرب
                baseImage = "/images/default-coat.jpg";
            }

            if (!string.IsNullOrEmpty(baseImage) && !baseImage.StartsWith("/"))
            {
                baseImage = "/" + baseImage;
            }

            return Json(new
            {
                success = true,
                baseImageUrl = baseImage,
                logoImageUrl = result.LogoImageUrl,
                embroideryPosition = result.EmbroideryPosition,
                basePrice = result.BasePrice,
                customizationFee = result.CustomizationFee,
                finalPrice = result.FinalPrice,
                basePriceFormatted = result.BasePrice.ToString("C"),
                customizationFeeFormatted = result.CustomizationFee.ToString("C"),
                finalPriceFormatted = result.FinalPrice.ToString("C")
            });
        }
        catch (KeyNotFoundException ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
    /// <summary>AJAX endpoint: securely uploads a customer's custom logo image.</summary>
    [HttpPost]
    [RequestSizeLimit(MaxLogoFileSizeBytes)]
    public async Task<IActionResult> UploadLogo(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return Json(new { success = false, message = "Please choose an image file." });

        if (file.Length > MaxLogoFileSizeBytes)
            return Json(new { success = false, message = "Logo image must be 5MB or smaller." });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedLogoExtensions.Contains(ext) || !AllowedLogoMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
            return Json(new { success = false, message = "Only PNG, JPG, or WebP images are allowed." });

        // Re-encode-free duplicate check: same size + same content hash avoids storing identical re-uploads twice.
        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer);
        buffer.Position = 0;
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(buffer.ToArray()));
        buffer.Position = 0;

        var existingUrl = HttpContext.Session.GetString($"logo_hash_{hash}");
        if (!string.IsNullOrEmpty(existingUrl))
            return Json(new { success = true, url = existingUrl });

        var url = await _fileStorage.UploadAsync(buffer, file.FileName, "images/customer-logos");
        HttpContext.Session.SetString($"logo_hash_{hash}", url);

        return Json(new { success = true, url });
    }

    /// <summary>AJAX endpoint: validates and adds the customized White Coat to the cart.</summary>
    [HttpPost]
    public async Task<IActionResult> AddToCart([FromBody] AddCustomizedItemToCartDto dto)
    {
        var validation = await _addToCartValidator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            return Json(new
            {
                success = false,
                errors = validation.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage })
            });
        }

        try
        {
            var cart = await _cartService.AddCustomizedItemToCartAsync(UserId, UserId == null ? SessionId : null, dto);
            return Json(new { success = true, itemCount = cart.ItemCount, message = "White Coat added to your cart!" });
        }
        catch (KeyNotFoundException ex)
        {
            return Json(new { success = false, errors = new[] { new { field = "", message = ex.Message } } });
        }
        catch (InvalidOperationException ex)
        {
            return Json(new { success = false, errors = new[] { new { field = "", message = ex.Message } } });
        }
    }
}
