using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopElegance.Core.DTOs;
using ShopElegance.Core.Interfaces.Services;

namespace ShopElegance.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,Manager")]
public class LogosController : Controller
{
    private readonly ILogoService _logoService;
    private readonly IFileStorageService _fileStorage;

    public LogosController(ILogoService logoService, IFileStorageService fileStorage)
    {
        _logoService = logoService;
        _fileStorage = fileStorage;
    }

    public async Task<IActionResult> Index()
    {
        var logos = await _logoService.GetAllLogosAsync();
        return View(logos);
    }

    public IActionResult Create() => View();

    [HttpPost]
    public async Task<IActionResult> Create(CreateLogoDto dto, IFormFile? imageFile)
    {
        if (imageFile == null || imageFile.Length == 0)
        {
            ModelState.AddModelError("ImageUrl", "A logo image is required.");
        }

        if (!ModelState.IsValid) return View(dto);

        using var stream = imageFile!.OpenReadStream();
        dto.ImageUrl = await _fileStorage.UploadAsync(stream, imageFile.FileName, "images/logos");

        await _logoService.CreateLogoAsync(dto);
        TempData["SuccessMessage"] = "Logo added.";
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Edit(int id)
    {
        var logos = await _logoService.GetAllLogosAsync();
        var logo = logos.FirstOrDefault(l => l.Id == id);
        if (logo == null) return NotFound();

        ViewBag.LogoId = id;
        ViewBag.CurrentImageUrl = logo.ImageUrl;
        return View(new CreateLogoDto { Name = logo.Name, Type = logo.Type, ImageUrl = logo.ImageUrl, IsActive = logo.IsActive });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, CreateLogoDto dto, IFormFile? imageFile)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.LogoId = id;
            ViewBag.CurrentImageUrl = dto.ImageUrl;
            return View(dto);
        }

        if (imageFile != null && imageFile.Length > 0)
        {
            using var stream = imageFile.OpenReadStream();
            dto.ImageUrl = await _fileStorage.UploadAsync(stream, imageFile.FileName, "images/logos");
        }

        await _logoService.UpdateLogoAsync(id, dto);
        TempData["SuccessMessage"] = "Logo updated.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        await _logoService.DeleteLogoAsync(id);
        TempData["SuccessMessage"] = "Logo deleted.";
        return RedirectToAction("Index");
    }
}
