using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToyStoreCore.Web.Data;
using ToyStoreCore.Web.Models;

namespace ToyStoreCore.Web.Controllers.Admin;

[Authorize(Roles = IdentitySeeder.AdminRole)]
[Route("Admin/Categories")]
public class AdminCategoriesController : Controller
{
    private readonly AppDbContext _db;

    public AdminCategoriesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var categories = await _db.Categories
            .Include(c => c.Products)
            .OrderBy(c => c.Name)
            .ToListAsync();
        return View(categories);
    }

    [HttpGet("Create")]
    public IActionResult Create()
    {
        return View(new Category());
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Category category)
    {
        if (!ModelState.IsValid)
        {
            return View(category);
        }

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        TempData["Message"] = "Đã thêm danh mục mới.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category is null)
        {
            return NotFound();
        }

        return View(category);
    }

    [HttpPost("Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Category category)
    {
        if (id != category.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(category);
        }

        _db.Categories.Update(category);
        await _db.SaveChangesAsync();

        TempData["Message"] = "Đã cập nhật danh mục.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _db.Categories.Include(c => c.Products).FirstOrDefaultAsync(c => c.Id == id);
        if (category is null)
        {
            return NotFound();
        }

        if (category.Products.Any())
        {
            TempData["Error"] = "Không thể xóa danh mục đang có sản phẩm. Hãy chuyển sản phẩm sang danh mục khác trước.";
            return RedirectToAction(nameof(Index));
        }

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();

        TempData["Message"] = "Đã xóa danh mục.";
        return RedirectToAction(nameof(Index));
    }
}
