using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopElegance.Core.DTOs;
using ShopElegance.Core.Interfaces.Services;

namespace ShopElegance.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,Manager")]
public class EmbroideryFontsController : Controller
{
    private readonly IEmbroideryService _embroideryService;

    public EmbroideryFontsController(IEmbroideryService embroideryService) => _embroideryService = embroideryService;

    public async Task<IActionResult> Index()
    {
        var fonts = await _embroideryService.GetAllFontsAsync();
        return View(fonts);
    }

    public IActionResult Create() => View(new CreateEmbroideryFontDto { IsActive = true });

    [HttpPost]
    public async Task<IActionResult> Create(CreateEmbroideryFontDto dto)
    {
        if (!ModelState.IsValid) return View(dto);

        await _embroideryService.CreateFontAsync(dto);
        TempData["SuccessMessage"] = "Embroidery font added.";
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Edit(int id)
    {
        var fonts = await _embroideryService.GetAllFontsAsync();
        var font = fonts.FirstOrDefault(f => f.Id == id);
        if (font == null) return NotFound();

        ViewBag.FontId = id;
        return View(new CreateEmbroideryFontDto
        {
            Name = font.Name,
            CssFontFamily = font.CssFontFamily,
            PreviewImageUrl = font.PreviewImageUrl,
            IsActive = font.IsActive
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, CreateEmbroideryFontDto dto)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.FontId = id;
            return View(dto);
        }

        await _embroideryService.UpdateFontAsync(id, dto);
        TempData["SuccessMessage"] = "Embroidery font updated.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        await _embroideryService.DeleteFontAsync(id);
        TempData["SuccessMessage"] = "Embroidery font deleted.";
        return RedirectToAction("Index");
    }
}
