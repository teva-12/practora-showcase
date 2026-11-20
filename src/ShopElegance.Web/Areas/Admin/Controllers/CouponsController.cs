using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopElegance.Core.DTOs;
using ShopElegance.Core.Interfaces.Services;

namespace ShopElegance.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,Manager")]
public class CouponsController : Controller
{
    private readonly ICouponService _couponService;

    public CouponsController(ICouponService couponService) => _couponService = couponService;

    public async Task<IActionResult> Index()
    {
        var coupons = await _couponService.GetAllCouponsAsync();
        return View(coupons);
    }

    public IActionResult Create() => View(new CreateCouponDto { IsActive = true, Type = "Percentage" });

    [HttpPost]
    public async Task<IActionResult> Create(CreateCouponDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        await _couponService.CreateCouponAsync(dto);
        TempData["SuccessMessage"] = "Coupon created successfully.";
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Edit(int id)
    {
        var coupons = await _couponService.GetAllCouponsAsync();
        var coupon = coupons.FirstOrDefault(c => c.Id == id);
        if (coupon == null) return NotFound();

        return View(new UpdateCouponDto
        {
            Id = coupon.Id, Code = coupon.Code, Description = coupon.Description,
            Type = coupon.Type, Value = coupon.Value,
            MinimumOrderAmount = coupon.MinimumOrderAmount,
            MaximumDiscountAmount = coupon.MaximumDiscountAmount,
            UsageLimit = coupon.UsageLimit, IsActive = coupon.IsActive,
            ExpiresAt = coupon.ExpiresAt
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, UpdateCouponDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        await _couponService.UpdateCouponAsync(id, dto);
        TempData["SuccessMessage"] = "Coupon updated.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        await _couponService.DeleteCouponAsync(id);
        TempData["SuccessMessage"] = "Coupon deleted.";
        return RedirectToAction("Index");
    }
}
