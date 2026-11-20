using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ShopElegance.Core.Entities;
using ShopElegance.Core.Interfaces.Services;

namespace ShopElegance.Web.Controllers;

[Authorize]
public class WishlistController : Controller
{
    private readonly IWishlistService _wishlistService;
    private readonly UserManager<ApplicationUser> _userManager;

    public WishlistController(IWishlistService wishlistService, UserManager<ApplicationUser> userManager)
    {
        _wishlistService = wishlistService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User)!;
        var wishlist = await _wishlistService.GetWishlistAsync(userId);
        return View(wishlist);
    }

    [HttpPost]
    public async Task<IActionResult> Toggle(int productId)
    {
        var userId = _userManager.GetUserId(User)!;
        bool isInWishlist = await _wishlistService.IsInWishlistAsync(userId, productId);

        if (isInWishlist)
            await _wishlistService.RemoveFromWishlistAsync(userId, productId);
        else
            await _wishlistService.AddToWishlistAsync(userId, productId);

        isInWishlist = !isInWishlist;
        var count = await _wishlistService.GetWishlistCountAsync(userId);

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return Json(new { success = true, inWishlist = isInWishlist, count });

        TempData[isInWishlist ? "SuccessMessage" : "InfoMessage"] =
            isInWishlist ? "Added to wishlist!" : "Removed from wishlist.";
        return RedirectBack();
    }

    [HttpPost]
    public async Task<IActionResult> Remove(int productId)
    {
        var userId = _userManager.GetUserId(User)!;
        await _wishlistService.RemoveFromWishlistAsync(userId, productId);
        TempData["SuccessMessage"] = "Item removed from wishlist.";
        return RedirectToAction("Index");
    }

    private IActionResult RedirectBack()
    {
        var referer = Request.Headers["Referer"].ToString();
        return !string.IsNullOrEmpty(referer) && Url.IsLocalUrl(referer)
            ? Redirect(referer)
            : RedirectToAction("Index");
    }
}
