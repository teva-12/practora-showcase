using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopElegance.Core.Interfaces.Services;

namespace ShopElegance.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,Manager")]
public class ReviewsController : Controller
{
    private readonly IReviewService _reviewService;

    public ReviewsController(IReviewService reviewService) => _reviewService = reviewService;

    public async Task<IActionResult> Index()
    {
        var reviews = await _reviewService.GetPendingReviewsAsync();
        return View(reviews);
    }

    [HttpPost]
    public async Task<IActionResult> Approve(int id)
    {
        await _reviewService.ApproveReviewAsync(id);
        TempData["SuccessMessage"] = "Review approved and published.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        await _reviewService.DeleteReviewAsync(id);
        TempData["SuccessMessage"] = "Review deleted.";
        return RedirectToAction("Index");
    }
}
