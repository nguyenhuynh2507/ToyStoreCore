using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToyStoreCore.Web.Data;
using ToyStoreCore.Web.Models;

namespace ToyStoreCore.Web.Controllers.Admin;

[Authorize(Roles = IdentitySeeder.AdminRole)]
[Route("Admin/Revenue")]
public class AdminRevenueController : Controller
{
    private readonly AppDbContext _db;

    public AdminRevenueController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(int? year, DateTime? startDate, DateTime? endDate)
    {
        DateTime rangeStart;
        DateTime rangeEnd;
        string filterMode;
        int? selectedYear;

        if (startDate.HasValue && endDate.HasValue)
        {
            filterMode = "DateRange";
            rangeStart = startDate.Value.Date;
            rangeEnd = endDate.Value.Date.AddDays(1).AddTicks(-1);
            selectedYear = year;
        }
        else
        {
            filterMode = "Year";
            selectedYear = year ?? DateTime.Now.Year;
            rangeStart = new DateTime(selectedYear.Value, 1, 1);
            rangeEnd = new DateTime(selectedYear.Value, 12, 31, 23, 59, 59);
            startDate = null;
            endDate = null;
        }

        var completedOrders = await _db.Orders
            .AsNoTracking()
            .Where(o => o.Status == OrderStatus.Completed && o.CreatedAt >= rangeStart && o.CreatedAt <= rangeEnd)
            .Select(o => new { o.Subtotal, o.MembershipDiscount, o.PromoDiscount, o.TotalAmount, o.PromoCode })
            .ToListAsync();

        ViewBag.FilterMode = filterMode;
        ViewBag.SelectedYear = selectedYear;
        ViewBag.StartDate = startDate;
        ViewBag.EndDate = endDate;
        ViewBag.RangeStart = rangeStart;
        ViewBag.RangeEnd = rangeEnd;

        ViewBag.OrderCount = completedOrders.Count;
        ViewBag.TotalSubtotal = completedOrders.Sum(o => o.Subtotal);
        ViewBag.TotalMembershipDiscount = completedOrders.Sum(o => o.MembershipDiscount);
        ViewBag.TotalPromoDiscount = completedOrders.Sum(o => o.PromoDiscount);
        ViewBag.TotalNetRevenue = completedOrders.Sum(o => o.TotalAmount);
        ViewBag.OrdersWithPromo = completedOrders.Count(o => !string.IsNullOrEmpty(o.PromoCode));
        ViewBag.OrdersWithMembershipDiscount = completedOrders.Count(o => o.MembershipDiscount > 0);

        return View();
    }
}
