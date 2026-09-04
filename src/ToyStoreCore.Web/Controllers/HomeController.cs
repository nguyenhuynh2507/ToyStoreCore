using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToyStoreCore.Web.Data;
using ToyStoreCore.Web.Models;
using ToyStoreCore.Web.Models.ViewModels;

namespace ToyStoreCore.Web.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _db;

    public HomeController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var featured = await _db.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.CreatedAt)
            .Take(8)
            .ToListAsync();

        var banners = await _db.Banners
            .Where(b => b.IsActive)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        return View(new HomeViewModel { FeaturedProducts = featured, Banners = banners });
    }

    public IActionResult Error()
    {
        return View();
    }
}
