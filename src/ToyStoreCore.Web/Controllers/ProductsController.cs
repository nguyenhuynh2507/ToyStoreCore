using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToyStoreCore.Web.Data;
using ToyStoreCore.Web.Models.ViewModels;
using ToyStoreCore.Web.Services;

namespace ToyStoreCore.Web.Controllers;

public class ProductsController : Controller
{
    private readonly AppDbContext _db;
    private readonly ActivityTrackingService _activityTracking;
    private readonly RecommendationService _recommendationService;

    public ProductsController(AppDbContext db, ActivityTrackingService activityTracking, RecommendationService recommendationService)
    {
        _db = db;
        _activityTracking = activityTracking;
        _recommendationService = recommendationService;
    }

    // GET /Products?categoryId=&q=&minPrice=&maxPrice=
    public async Task<IActionResult> Index(int? categoryId, string? q, decimal? minPrice, decimal? maxPrice)
    {
        var query = _db.Products.Include(p => p.Category).Where(p => p.IsActive);

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(p => p.Name.Contains(q));
        }

        if (minPrice.HasValue)
        {
            query = query.Where(p => p.Price >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(p => p.Price <= maxPrice.Value);
        }

        ViewBag.Categories = await _db.Categories.OrderBy(c => c.Name).ToListAsync();
        ViewBag.SelectedCategoryId = categoryId;
        ViewBag.Query = q;
        ViewBag.MinPrice = minPrice;
        ViewBag.MaxPrice = maxPrice;

        var products = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
        return View(products);
    }

    // GET /Products/SearchSuggestions?query=
    [HttpGet]
    public async Task<IActionResult> SearchSuggestions(string? query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
        {
            return Json(Array.Empty<string>());
        }

        var names = await _db.Products
            .Where(p => p.IsActive && p.Name.Contains(query))
            .OrderBy(p => p.Name)
            .Select(p => p.Name)
            .Take(8)
            .ToListAsync();

        return Json(names);
    }

    // GET /Products/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var product = await _db.Products
            .Include(p => p.Category)
            .Include(p => p.Images.OrderBy(i => i.DisplayOrder))
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

        if (product is null)
        {
            return NotFound();
        }

        var userId = User.Identity?.IsAuthenticated == true
            ? User.FindFirstValue(ClaimTypes.NameIdentifier)
            : null;
        await _activityTracking.TrackAsync(userId, "ViewProduct", product.Id);

        var recommendedIds = await _recommendationService.GetRecommendedProductIdsAsync(product.Id);
        var recommended = recommendedIds.Count == 0
            ? new List<Models.Product>()
            : await _db.Products.Where(p => recommendedIds.Contains(p.Id) && p.IsActive).ToListAsync();

        return View(new ProductDetailsViewModel { Product = product, RecommendedProducts = recommended });
    }
}
