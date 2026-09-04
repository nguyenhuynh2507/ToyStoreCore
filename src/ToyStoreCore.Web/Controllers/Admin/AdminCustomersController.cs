using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToyStoreCore.Web.Data;

namespace ToyStoreCore.Web.Controllers.Admin;

[Authorize(Roles = IdentitySeeder.AdminRole)]
[Route("Admin/Customers")]
public class AdminCustomersController : Controller
{
    private readonly AppDbContext _db;

    public AdminCustomersController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? searchString)
    {
        ViewData["CurrentFilter"] = searchString;

        var query = _db.Users.Include(u => u.Tier).AsQueryable();
        if (!string.IsNullOrEmpty(searchString))
        {
            query = query.Where(u =>
                u.FullName.Contains(searchString) || (u.Email != null && u.Email.Contains(searchString)));
        }

        var customers = await query.OrderByDescending(u => u.TotalCompletedOrders).ToListAsync();
        return View(customers);
    }

    [HttpGet("Details/{id}")]
    public async Task<IActionResult> Details(string id)
    {
        var customer = await _db.Users.Include(u => u.Tier).FirstOrDefaultAsync(u => u.Id == id);
        if (customer is null)
        {
            return NotFound();
        }

        var orders = await _db.Orders
            .Include(o => o.Items)
            .Where(o => o.UserId == id)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        ViewBag.Orders = orders;
        return View(customer);
    }

    [HttpPost("ToggleLock/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLock(string id)
    {
        var customer = await _db.Users.FindAsync(id);
        if (customer is null)
        {
            return NotFound();
        }

        customer.IsLocked = !customer.IsLocked;
        await _db.SaveChangesAsync();

        TempData["Message"] = customer.IsLocked
            ? "Đã khóa tài khoản khách hàng."
            : "Đã mở khóa tài khoản khách hàng.";
        return RedirectToAction(nameof(Details), new { id });
    }
}
