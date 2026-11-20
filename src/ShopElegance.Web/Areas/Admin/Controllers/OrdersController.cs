using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ShopElegance.Core.DTOs;
using ShopElegance.Core.Entities;
using ShopElegance.Core.Enums;
using ShopElegance.Core.Interfaces.Services;

namespace ShopElegance.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,Manager")]
public class OrdersController : Controller
{
    private readonly IOrderService _orderService;
    private readonly UserManager<ApplicationUser> _userManager;

    public OrdersController(IOrderService orderService, UserManager<ApplicationUser> userManager)
    {
        _orderService = orderService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(
        int page = 1, string? status = null, string? search = null,
        DateTime? from = null, DateTime? to = null)
    {
        var orders = await _orderService.GetAllOrdersAsync(new OrderFilterDto
        {
            Page = page, PageSize = 20,
            Status = status, Search = search, From = from, To = to
        });

        ViewBag.Status = status;
        ViewBag.Search = search;
        ViewBag.From = from;
        ViewBag.To = to;
        ViewBag.Statuses = Enum.GetNames<OrderStatus>();
        return View(orders);
    }

    public async Task<IActionResult> Detail(int id)
    {
        var order = await _orderService.GetOrderByIdAsync(id);
        if (order == null) return NotFound();
        ViewBag.Statuses = Enum.GetNames<OrderStatus>();
        ViewBag.PaymentStatuses = Enum.GetNames<PaymentStatus>();
        return View(order);
    }

    /// <summary>Print-friendly order view for warehouse/fulfillment staff.</summary>
    public async Task<IActionResult> Print(int id)
    {
        var order = await _orderService.GetOrderByIdAsync(id);
        if (order == null) return NotFound();
        return View(order);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateStatus(int id, string status, string? paymentStatus,
        string? comment, string? trackingNumber)
    {
        if (!Enum.TryParse<OrderStatus>(status, out var orderStatus))
        {
            TempData["ErrorMessage"] = "Invalid status.";
            return RedirectToAction("Detail", new { id });
        }

        PaymentStatus? ps = null;
        if (!string.IsNullOrEmpty(paymentStatus) && Enum.TryParse<PaymentStatus>(paymentStatus, out var parsed))
            ps = parsed;

        var userId = _userManager.GetUserId(User);
        await _orderService.UpdateOrderStatusAsync(id, new UpdateOrderStatusDto
        {
            Status = orderStatus, PaymentStatus = ps,
            Comment = comment, TrackingNumber = trackingNumber,
            ChangedByUserId = userId
        });

        TempData["SuccessMessage"] = "Order status updated.";
        return RedirectToAction("Detail", new { id });
    }
}
