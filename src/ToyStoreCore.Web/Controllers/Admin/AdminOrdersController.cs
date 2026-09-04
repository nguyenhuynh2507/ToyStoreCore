using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToyStoreCore.Web.Data;
using ToyStoreCore.Web.Models;
using ToyStoreCore.Web.Services;

namespace ToyStoreCore.Web.Controllers.Admin;

[Authorize(Roles = IdentitySeeder.AdminRole)]
[Route("Admin/Orders")]
public class AdminOrdersController : Controller
{
    private readonly AppDbContext _db;
    private readonly MembershipService _membershipService;

    public AdminOrdersController(AppDbContext db, MembershipService membershipService)
    {
        _db = db;
        _membershipService = membershipService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(OrderStatus? status)
    {
        var query = _db.Orders.Include(o => o.Items).AsQueryable();
        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        ViewBag.SelectedStatus = status;
        var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();
        return View(orders);
    }

    [HttpGet("Details/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var order = await _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);
        if (order is null)
        {
            return NotFound();
        }

        return View(order);
    }

    [HttpPost("UpdateStatus/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, OrderStatus status)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order is null)
        {
            return NotFound();
        }

        var oldStatus = order.Status;
        order.Status = status;
        await _db.SaveChangesAsync();

        // Membership tier progress: only count a completed order once, mirroring the old
        // ToyStore guard (oldStatus != Completed) so re-saving "Completed" doesn't double-count.
        if (status == OrderStatus.Completed && oldStatus != OrderStatus.Completed && order.UserId is not null)
        {
            await _membershipService.RecalculateTierAsync(order.UserId);
        }

        TempData["Message"] = "Đã cập nhật trạng thái đơn hàng.";
        return RedirectToAction(nameof(Details), new { id });
    }
}
