using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToyStoreCore.Web.Data;
using ToyStoreCore.Web.Models;

namespace ToyStoreCore.Web.Controllers.Admin;

[Authorize(Roles = IdentitySeeder.AdminRole)]
[Route("Admin/Banners")]
public class AdminBannersController : Controller
{
    private readonly AppDbContext _db;

    public AdminBannersController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var banners = await _db.Banners
            .OrderByDescending(b => b.IsActive)
            .ThenByDescending(b => b.CreatedAt)
            .ToListAsync();
        return View(banners);
    }

    [HttpGet("Create")]
    public IActionResult Create()
    {
        return View(new Banner { IsActive = true });
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Banner banner)
    {
        if (!ModelState.IsValid)
        {
            return View(banner);
        }

        banner.CreatedAt = DateTime.UtcNow;
        _db.Banners.Add(banner);
        await _db.SaveChangesAsync();

        TempData["Message"] = "Đã thêm banner mới.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var banner = await _db.Banners.FindAsync(id);
        if (banner is null)
        {
            return NotFound();
        }

        return View(banner);
    }

    [HttpPost("Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Banner banner)
    {
        if (id != banner.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(banner);
        }

        var existing = await _db.Banners.FindAsync(id);
        if (existing is null)
        {
            return NotFound();
        }

        existing.Title = banner.Title;
        existing.ImageUrl = banner.ImageUrl;
        existing.LinkUrl = banner.LinkUrl;
        existing.IsActive = banner.IsActive;
        await _db.SaveChangesAsync();

        TempData["Message"] = "Đã cập nhật banner.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var banner = await _db.Banners.FindAsync(id);
        if (banner is not null)
        {
            _db.Banners.Remove(banner);
            await _db.SaveChangesAsync();
        }

        TempData["Message"] = "Đã xóa banner.";
        return RedirectToAction(nameof(Index));
    }
}
