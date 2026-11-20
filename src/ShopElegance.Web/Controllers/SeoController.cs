using Microsoft.AspNetCore.Mvc;
using ShopElegance.Core.Interfaces.Services;
using System.Text;
using System.Xml;

namespace ShopElegance.Web.Controllers;

[Route("[controller]")]
public class SeoController : Controller
{
    private readonly IProductService _productService;
    private readonly ICategoryService _categoryService;
    private readonly IConfiguration _config;

    public SeoController(
        IProductService productService,
        ICategoryService categoryService,
        IConfiguration config)
    {
        _productService  = productService;
        _categoryService = categoryService;
        _config          = config;
    }

    [HttpGet("/sitemap.xml")]
    public async Task<IActionResult> Sitemap()
    {
        var baseUrl = _config["AppSettings:SiteUrl"] ?? "https://practora.com";

        var settings = new XmlWriterSettings
        {
            Async     = true,
            Encoding  = Encoding.UTF8,
            Indent    = true
        };

        using var ms     = new MemoryStream();
        await using var writer = XmlWriter.Create(ms, settings);

        await writer.WriteStartDocumentAsync();
        await writer.WriteStartElementAsync(null, "urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");

        // Static pages
        foreach (var url in new[] { "", "/Shop", "/Account/Login", "/Account/Register", "/Home/Privacy" })
        {
            await WriteUrl(writer, baseUrl + url, DateTime.UtcNow, "weekly", url == "" ? "1.0" : "0.8");
        }

        // Categories
        var categories = await _categoryService.GetActiveCategoriesAsync();
        foreach (var cat in categories)
        {
            await WriteUrl(writer,
                $"{baseUrl}/Shop?categoryId={cat.Id}",
                DateTime.UtcNow, "weekly", "0.7");
        }

        // Products
        var products = await _productService.GetProductsAsync(new Core.DTOs.ProductFilterDto
        {
            Page = 1, PageSize = 1000
        });
        foreach (var p in products.Items.Where(p => p.IsActive))
        {
            await WriteUrl(writer,
                $"{baseUrl}/Shop/Product/{p.Slug}",
                DateTime.UtcNow, "weekly", "0.9");
        }

        await writer.WriteEndElementAsync(); // </urlset>
        await writer.WriteEndDocumentAsync();
        await writer.FlushAsync();

        return File(ms.ToArray(), "application/xml");
    }

    [HttpGet("/robots.txt")]
    public IActionResult Robots()
    {
        var baseUrl = _config["AppSettings:SiteUrl"] ?? "https://practora.com";
        var content = $"""
            User-agent: *
            Allow: /
            Disallow: /Admin/
            Disallow: /Account/Profile
            Disallow: /Account/Orders
            Disallow: /Checkout
            Disallow: /Cart
            Disallow: /Wishlist
            Disallow: /Account/ChangePassword
            Disallow: /Account/AccessDenied
            Crawl-delay: 1

            User-agent: GPTBot
            Disallow: /

            Sitemap: {baseUrl}/sitemap.xml
            """;

        return Content(content, "text/plain");
    }

    private static async Task WriteUrl(
        XmlWriter writer, string loc, DateTime lastMod,
        string changefreq, string priority)
    {
        await writer.WriteStartElementAsync(null, "url", null);
        await writer.WriteElementStringAsync(null, "loc",        null, loc);
        await writer.WriteElementStringAsync(null, "lastmod",    null, lastMod.ToString("yyyy-MM-dd"));
        await writer.WriteElementStringAsync(null, "changefreq", null, changefreq);
        await writer.WriteElementStringAsync(null, "priority",   null, priority);
        await writer.WriteEndElementAsync();
    }
}
