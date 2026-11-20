using Microsoft.AspNetCore.Mvc;
using ShopElegance.Core.Interfaces.Services;

namespace ShopElegance.Web.Controllers;

public class CartController : Controller
{
    private readonly ICartService _cartService;
    private readonly ICouponService _couponService;

    public CartController(ICartService cartService, ICouponService couponService)
    {
        _cartService = cartService;
        _couponService = couponService;
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

    public async Task<IActionResult> Index()
    {
        var cart = await _cartService.GetCartAsync(UserId, UserId == null ? SessionId : null);
        return View(cart);
    }

    [HttpPost]
    public async Task<IActionResult> Add(int productId, int quantity = 1)
    {
        var cart = await _cartService.AddToCartAsync(UserId, UserId == null ? SessionId : null, productId, quantity);
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Json(new { success = true, itemCount = cart.ItemCount, message = "Item added to cart!" });
        }
        TempData["SuccessMessage"] = "Item added to cart!";
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> Update(int cartItemId, int quantity)
    {
        var cart = await _cartService.UpdateCartItemAsync(UserId, UserId == null ? SessionId : null, cartItemId, quantity);
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Json(new
            {
                success = true,
                itemCount = cart.ItemCount,
                subTotal = cart.SubTotal.ToString("C"),
                items = cart.Items.Select(i => new { id = i.Id, totalPrice = i.TotalPrice.ToString("C") })
            });
        }
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> Remove(int cartItemId)
    {
        var cart = await _cartService.RemoveFromCartAsync(UserId, UserId == null ? SessionId : null, cartItemId);
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return Json(new { success = true, itemCount = cart.ItemCount, subTotal = cart.SubTotal.ToString("C") });
        TempData["SuccessMessage"] = "Item removed from cart.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> ApplyCoupon(string couponCode, decimal orderTotal)
    {
        var result = await _couponService.ValidateCouponAsync(couponCode, orderTotal);
        return Json(new
        {
            success = result.IsValid,
            message = result.ErrorMessage ?? $"Coupon applied! You save {result.DiscountAmount:C}",
            discountAmount = result.DiscountAmount
        });
    }

    [HttpPost]
    public async Task<IActionResult> Clear()
    {
        await _cartService.ClearCartAsync(UserId, UserId == null ? SessionId : null);
        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> Count()
    {
        var count = await _cartService.GetCartItemCountAsync(UserId, UserId == null ? SessionId : null);
        return Json(new { count });
    }
}
