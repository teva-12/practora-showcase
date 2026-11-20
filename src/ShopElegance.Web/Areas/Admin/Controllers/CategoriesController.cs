using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopElegance.Core.DTOs;
using ShopElegance.Core.Interfaces.Services;

namespace ShopElegance.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,Manager")]
public class CategoriesController : Controller
{
    private readonly ICategoryService _categoryService;
    private readonly IFileStorageService _fileStorage;

    public CategoriesController(ICategoryService categoryService, IFileStorageService fileStorage)
    {
        _categoryService = categoryService;
        _fileStorage = fileStorage;
    }

    public async Task<IActionResult> Index()
    {
        var categories = await _categoryService.GetAllCategoriesAsync();
        return View(categories);
    }

    public async Task<IActionResult> Create()
    {
        // عرض الكليات الرئيسية فقط في القائمة المنسدلة للـ Parent
        var categories = await _categoryService.GetAllCategoriesAsync();
        ViewBag.ParentCategories = categories.Where(c => c.ParentCategoryId == null);

        return View(new CreateCategoryDto { IsActive = true });
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCategoryDto dto)
    {
        if (!ModelState.IsValid)
        {
            var categories = await _categoryService.GetAllCategoriesAsync();
            ViewBag.ParentCategories = categories.Where(c => c.ParentCategoryId == null);
            return View(dto);
        }

        await _categoryService.CreateCategoryAsync(dto);
        TempData["SuccessMessage"] = "Category created successfully.";
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Edit(int id)
    {
        var categories = await _categoryService.GetAllCategoriesAsync();
        var cat = categories.FirstOrDefault(c => c.Id == id);
        if (cat == null) return NotFound();

        // عرض الكليات الرئيسية فقط مع استبعاد القسم الحالي منعاً للـ Circular Dependency
        ViewBag.ParentCategories = categories.Where(c => c.ParentCategoryId == null && c.Id != id);
        ViewBag.CategoryId = id;

        return View(new UpdateCategoryDto
        {
            Id = cat.Id,
            Name = cat.Name,
            Description = cat.Description,
            ImageUrl = cat.ImageUrl,
            ParentCategoryId = cat.ParentCategoryId,
            IsActive = cat.IsActive,
            SortOrder = cat.SortOrder
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, UpdateCategoryDto dto)
    {
        if (!ModelState.IsValid)
        {
            var categories = await _categoryService.GetAllCategoriesAsync();
            ViewBag.ParentCategories = categories.Where(c => c.ParentCategoryId == null && c.Id != id);
            ViewBag.CategoryId = id;
            return View(dto);
        }

        await _categoryService.UpdateCategoryAsync(id, dto);
        TempData["SuccessMessage"] = "Category updated.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        await _categoryService.DeleteCategoryAsync(id);
        TempData["SuccessMessage"] = "Category deleted.";
        return RedirectToAction("Index");
    }
}