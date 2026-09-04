using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToyStoreCore.Web.Data;
using ToyStoreCore.Web.Models.ViewModels;

namespace ToyStoreCore.Web.Controllers.Admin;

[Authorize(Roles = IdentitySeeder.AdminRole)]
[Route("Admin/ActivityLog")]
public class AdminActivityLogController : Controller
{
    private const int PageSize = 30;

    private readonly AppDbContext _db;

    public AdminActivityLogController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? activityType, int page = 1)
    {
        page = page < 1 ? 1 : page;

        var query = _db.UserActivityLogs.Include(l => l.User).Include(l => l.Product).AsQueryable();
        if (!string.IsNullOrEmpty(activityType))
        {
            query = query.Where(l => l.ActivityType == activityType);
        }

        var totalCount = await query.CountAsync();
        var logs = await query
            .OrderByDescending(l => l.Timestamp)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        ViewBag.SelectedActivityType = activityType;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)PageSize);

        return View(logs);
    }
}
