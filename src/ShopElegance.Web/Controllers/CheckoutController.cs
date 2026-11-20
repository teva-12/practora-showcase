using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ShopElegance.Core.DTOs;
using ShopElegance.Core.Entities;
using ShopElegance.Core.Interfaces.Services;
using ShopElegance.Web.ViewModels;

namespace ShopElegance.Web.Controllers;

[Authorize]
public class CheckoutController : Controller
{
    private readonly ICartService _cartService;
    private readonly IOrderService _orderService;
    private readonly ICouponService _couponService;
    private readonly IShippingCalculationService _shippingService;
    private readonly UserManager<ApplicationUser> _userManager;

    public CheckoutController(
        ICartService cartService,
        IOrderService orderService,
        ICouponService couponService,
        IShippingCalculationService shippingService,
        UserManager<ApplicationUser> userManager)
    {
        _cartService = cartService;
        _orderService = orderService;
        _couponService = couponService;
        _shippingService = shippingService;
        _userManager = userManager;
    }

    
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var cart = await _cartService.GetCartAsync(user.Id, null);
        if (cart == null || !cart.Items.Any())
        {
            TempData["ErrorMessage"] = "Your cart is empty.";
            return RedirectToAction("Index", "Cart");
        }

        // حساب الشحن المبدئي لو للمستخدم عنوان سابق أو افتراضي
        var defaultGov = string.Empty;
        var defaultCity = string.Empty;

        var initialShipping = _shippingService.Calculate(defaultGov, cart.SubTotal, defaultCity);

        var vm = new CheckoutViewModel
        {
            Cart = cart,
            Governorates = _shippingService.GetGovernorates(),
            ShippingCost = initialShipping.ShippingCost, // تعيين الشحن المبدئي
            ShippingAddress = new CheckoutAddressViewModel
            {
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Phone = user.PhoneNumber ?? string.Empty
            },
            Customizations = BuildCustomizationEditVms(cart)
        };

        return View(vm);
    }

    /// <summary>
    /// AJAX endpoint: Recalculates shipping cost and ETA based on governorate, city, and subtotal.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CalculateShipping(string governorate, string? city = null)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var cart = await _cartService.GetCartAsync(user.Id, null);
        if (cart == null)
        {
            return Json(new { success = false, message = "Cart not found." });
        }

        var result = _shippingService.Calculate(governorate, cart.SubTotal, city);

        return Json(new
        {
            success = result.IsValid,
            message = result.ErrorMessage,
            shippingCost = result.ShippingCost,
            shippingCostFormatted = result.ShippingCost.ToString("C"),
            estimatedDeliveryDays = result.EstimatedDeliveryDays,
            isFreeLocalDelivery = result.IsFreeLocalDelivery
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder(CheckoutViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var cart = await _cartService.GetCartAsync(user.Id, null);
        if (cart == null || !cart.Items.Any())
        {
            TempData["ErrorMessage"] = "Your cart is empty.";
            return RedirectToAction("Index", "Cart");
        }

        model.Cart = cart;
        model.Governorates = _shippingService.GetGovernorates();

        // Validate shipping rules server-side
        var shipping = _shippingService.Calculate(
            model.ShippingAddress?.State ?? string.Empty,
            cart.SubTotal,
            model.ShippingAddress?.City
        );

        if (!shipping.IsValid)
        {
            ModelState.AddModelError("ShippingAddress.State", shipping.ErrorMessage ?? "Please select a valid governorate.");
        }

        if (!ModelState.IsValid)
        {
            model.Customizations = BuildCustomizationEditVms(cart, model.Customizations);
            return View("Index", model);
        }

        var addr = model.ShippingAddress!;

        // Safely extract custom adjustments matching CartItemId
        var editsByCartItemId = (model.Customizations ?? new List<CheckoutCustomizationEditViewModel>())
            .GroupBy(c => c.CartItemId)
            .ToDictionary(g => g.Key, g => g.First());

        var createOrderDto = new CreateOrderDto
        {
            UserId = user.Id,
            ShippingAddress = new ShippingAddressDto
            {
                FirstName = addr.FirstName,
                LastName = addr.LastName,
                Email = addr.Email,
                Phone = addr.Phone,
                AddressLine1 = addr.AddressLine1,
                AddressLine2 = addr.AddressLine2,
                City = addr.City,
                State = addr.State,
                PostalCode = addr.PostalCode,
                Country = addr.Country
            },
            Items = cart.Items.Select(i =>
            {
                CreateCustomizationDto? customizationDto = null;
                if (i.Customization != null)
                {
                    editsByCartItemId.TryGetValue(i.Id, out var edit);
                    customizationDto = new CreateCustomizationDto
                    {
                        WhiteCoatVariantId = i.Customization.WhiteCoatVariantId,
                        EmbroideryEnabled = i.Customization.EmbroideryEnabled,
                        StudentName = edit?.StudentName ?? i.Customization.StudentName,
                        DoctorName = edit?.DoctorName ?? i.Customization.DoctorName,
                        Faculty = edit?.Faculty ?? i.Customization.Faculty,
                        Department = edit?.Department ?? i.Customization.Department,
                        GraduationYear = edit?.GraduationYear ?? i.Customization.GraduationYear,
                        EmbroideryTextColor = edit?.EmbroideryTextColor ?? i.Customization.EmbroideryTextColor,
                        EmbroideryFontId = i.Customization.EmbroideryFontId,
                        EmbroideryPosition = i.Customization.EmbroideryPosition,
                        LogoType = i.Customization.LogoType,
                        LogoId = i.Customization.LogoId,
                        CoatType = i.Customization.CoatType,
                        Size = i.Customization.Size,
                        Fabric = i.Customization.Fabric,
                        Color = i.Customization.Color,
                        CustomLogoUrl = i.Customization.CustomLogoUrl
                    };
                }

                return new CreateOrderItemDto
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    WhiteCoatVariantId = i.WhiteCoatVariantId,
                    Customization = customizationDto
                };
            }).ToList(),
            CouponCode = model.CouponCode,
            ShippingCost = shipping.ShippingCost,
            TaxAmount = 0, // Calculated below
            PaymentMethod = model.PaymentMethod,
            Notes = null
        };

        // Server-side recalculation of estimated discount for accurate tax calculation
        decimal estimatedDiscount = 0;
        if (!string.IsNullOrWhiteSpace(model.CouponCode))
        {
            var couponPreview = await _couponService.ValidateCouponAsync(model.CouponCode, cart.SubTotal);
            if (couponPreview != null && couponPreview.IsValid)
            {
                estimatedDiscount = couponPreview.DiscountAmount;
            }
        }

        // Calculate tax safely
        decimal taxableBase = Math.Max(0, cart.SubTotal - estimatedDiscount + shipping.ShippingCost);
        createOrderDto.TaxAmount = Math.Round(taxableBase * model.TaxRate, 2);

        try
        {
            var order = await _orderService.CreateOrderAsync(createOrderDto);
            await _cartService.ClearCartAsync(user.Id, null);
            return RedirectToAction("Confirmation", new { orderNumber = order.OrderNumber });
        }
        catch (Exception ex) when (ex is InvalidOperationException || ex is KeyNotFoundException)
        {
            TempData["ErrorMessage"] = ex.Message;
            model.Customizations = BuildCustomizationEditVms(cart, model.Customizations);
            return View("Index", model);
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "An error occurred while processing your order. Please try again.";
            model.Customizations = BuildCustomizationEditVms(cart, model.Customizations);
            return View("Index", model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Confirmation(string orderNumber)
    {
        if (string.IsNullOrWhiteSpace(orderNumber)) return NotFound();

        var order = await _orderService.GetOrderByNumberAsync(orderNumber);
        if (order == null) return NotFound();

        var userId = _userManager.GetUserId(User);
        if (order.UserId != userId) return Forbid();

        return View(order);
    }

    private static List<CheckoutCustomizationEditViewModel> BuildCustomizationEditVms(
        CartDto cart, List<CheckoutCustomizationEditViewModel>? existingEdits = null)
    {
        var editsById = (existingEdits ?? new List<CheckoutCustomizationEditViewModel>())
            .GroupBy(e => e.CartItemId)
            .ToDictionary(g => g.Key, g => g.First());

        return cart.Items
            .Where(i => i.Customization != null)
            .Select(i =>
            {
                if (editsById.TryGetValue(i.Id, out var existing)) return existing;

                return new CheckoutCustomizationEditViewModel
                {
                    CartItemId = i.Id,
                    ProductName = i.ProductName,
                    VariantSummary = i.Customization!.EmbroideryFontName != null
                        ? $"Embroidered · {i.Customization.EmbroideryPosition}"
                        : "Customized",
                    StudentName = i.Customization.StudentName,
                    DoctorName = i.Customization.DoctorName,
                    Faculty = i.Customization.Faculty,
                    Department = i.Customization.Department,
                    GraduationYear = i.Customization.GraduationYear,
                    EmbroideryTextColor = i.Customization.EmbroideryTextColor,
                    CustomLogoUrl = i.Customization.CustomLogoUrl
                };
            }).ToList();
    }
}