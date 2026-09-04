using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToyStoreCore.Web.Data;
using ToyStoreCore.Web.Models;
using ToyStoreCore.Web.Models.ViewModels;

namespace ToyStoreCore.Web.Controllers.Admin;

[Authorize(Roles = IdentitySeeder.AdminRole)]
[Route("Admin/Profit")]
public class AdminProfitController : Controller
{
    private readonly AppDbContext _db;

    public AdminProfitController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(int? month, int? year)
    {
        var filterMonth = month is >= 1 and <= 12 ? month.Value : DateTime.Now.Month;
        var filterYear = year is >= 2000 and <= 2100 ? year.Value : DateTime.Now.Year;

        var periodStart = new DateTime(filterYear, filterMonth, 1);
        var periodEnd = periodStart.AddMonths(1);

        var totalRevenue = await _db.Orders
            .AsNoTracking()
            .Where(o => o.Status == OrderStatus.Completed && o.CreatedAt >= periodStart && o.CreatedAt < periodEnd)
            .SumAsync(o => o.TotalAmount);

        var totalCogs = await _db.GoodsReceipts
            .AsNoTracking()
            .Where(r => r.ImportDate >= periodStart && r.ImportDate < periodEnd)
            .SumAsync(r => r.TotalCost);

        return View(new ProfitReportViewModel
        {
            Month = filterMonth,
            Year = filterYear,
            TotalRevenue = totalRevenue,
            TotalCogs = totalCogs,
        });
    }
}
