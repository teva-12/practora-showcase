using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopElegance.Core.Interfaces.Services;

namespace ShopElegance.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,Manager")]
public class DashboardController : Controller
{
    private readonly IOrderService _orderService;
    private readonly IProductService _productService;
    private readonly IWhiteCoatVariantService _whiteCoatVariantService;

    public DashboardController(IOrderService orderService, IProductService productService, IWhiteCoatVariantService whiteCoatVariantService)
    {
        _orderService = orderService;
        _productService = productService;
        _whiteCoatVariantService = whiteCoatVariantService;
    }

    public async Task<IActionResult> Index()
    {
        var stats = await _orderService.GetDashboardStatsAsync();
        ViewBag.LowStockWhiteCoatVariants = await _whiteCoatVariantService.GetLowStockVariantsAsync();
        return View(stats);
    }
}
