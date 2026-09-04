using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToyStoreCore.Web.Data;
using ToyStoreCore.Web.Models;

namespace ToyStoreCore.Web.Controllers.Admin;

[Authorize(Roles = IdentitySeeder.AdminRole)]
[Route("Admin/Statistics")]
public class AdminStatisticsController : Controller
{
    private readonly AppDbContext _db;
    private readonly ILogger<AdminStatisticsController> _logger;

    public AdminStatisticsController(AppDbContext db, ILogger<AdminStatisticsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet("")]
    public IActionResult Index()
    {
        ViewBag.DefaultYear = DateTime.Now.Year;
        return View();
    }

    private record RevenuePoint(string Label, int Year, int TotalOrders, decimal TotalRevenue);

    [HttpGet("GetRevenueData")]
    public async Task<IActionResult> GetRevenueData(string type, int? year)
    {
        try
        {
            var normalizedType = (type ?? string.Empty).Trim().ToLowerInvariant();
            var completedOrders = _db.Orders.AsNoTracking().Where(o => o.Status == OrderStatus.Completed);

            List<RevenuePoint> items;

            switch (normalizedType)
            {
                case "month":
                {
                    if (!year.HasValue || year is < 2000 or > 2100)
                    {
                        return BadRequest(new { success = false, message = "Vui lòng chọn năm hợp lệ (2000–2100)." });
                    }

                    var rows = await completedOrders
                        .Where(o => o.CreatedAt.Year == year.Value)
                        .GroupBy(o => o.CreatedAt.Month)
                        .Select(g => new { Month = g.Key, Count = g.Count(), Revenue = g.Sum(o => o.TotalAmount) })
                        .ToListAsync();

                    var lookup = rows.ToDictionary(r => r.Month);
                    items = Enumerable.Range(1, 12)
                        .Select(m => lookup.TryGetValue(m, out var r)
                            ? new RevenuePoint($"Tháng {m}", year.Value, r.Count, r.Revenue)
                            : new RevenuePoint($"Tháng {m}", year.Value, 0, 0m))
                        .ToList();
                    break;
                }

                case "quarter":
                {
                    if (!year.HasValue || year is < 2000 or > 2100)
                    {
                        return BadRequest(new { success = false, message = "Vui lòng chọn năm hợp lệ (2000–2100)." });
                    }

                    var rows = await completedOrders
                        .Where(o => o.CreatedAt.Year == year.Value)
                        .Select(o => new { o.CreatedAt.Month, o.TotalAmount })
                        .ToListAsync();

                    items = rows
                        .GroupBy(o => (o.Month - 1) / 3 + 1)
                        .OrderBy(g => g.Key)
                        .Select(g => new RevenuePoint($"Quý {g.Key}", year.Value, g.Count(), g.Sum(o => o.TotalAmount)))
                        .ToList();
                    break;
                }

                case "year":
                {
                    var rows = await completedOrders
                        .GroupBy(o => o.CreatedAt.Year)
                        .Select(g => new { Year = g.Key, Count = g.Count(), Revenue = g.Sum(o => o.TotalAmount) })
                        .OrderBy(g => g.Year)
                        .ToListAsync();

                    items = rows.Select(r => new RevenuePoint(r.Year.ToString(), r.Year, r.Count, r.Revenue)).ToList();
                    break;
                }

                default:
                    return BadRequest(new { success = false, message = "Tham số type không hợp lệ. Dùng: month, quarter, year." });
            }

            return Json(new
            {
                success = true,
                type = normalizedType,
                year,
                items = items.Select(i => new { label = i.Label, year = i.Year, totalOrders = i.TotalOrders, totalRevenue = i.TotalRevenue }),
                summary = new
                {
                    totalOrders = items.Sum(i => i.TotalOrders),
                    totalRevenue = items.Sum(i => i.TotalRevenue),
                },
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetRevenueData failed. type={Type}, year={Year}", type, year);
            return StatusCode(500, new { success = false, message = "Không thể tải dữ liệu thống kê." });
        }
    }

    [HttpGet("GetTopProductsData")]
    public async Task<IActionResult> GetTopProductsData(int topN = 5)
    {
        var data = await _db.OrderItems
            .AsNoTracking()
            .Where(oi => oi.Order!.Status == OrderStatus.Completed)
            .GroupBy(oi => new { oi.ProductId, oi.ProductName })
            .Select(g => new
            {
                productId = g.Key.ProductId,
                productName = g.Key.ProductName,
                totalQuantity = g.Sum(x => x.Quantity),
                totalRevenue = g.Sum(x => x.Quantity * x.UnitPrice),
            })
            .OrderByDescending(x => x.totalQuantity)
            .Take(topN)
            .ToListAsync();

        return Json(new { success = true, items = data });
    }

    [HttpGet("GetTopCustomersData")]
    public async Task<IActionResult> GetTopCustomersData(int topN = 5)
    {
        var data = await _db.Orders
            .AsNoTracking()
            .Where(o => o.Status == OrderStatus.Completed && o.UserId != null)
            .Include(o => o.User)
            .GroupBy(o => new { o.UserId, o.User!.FullName })
            .Select(g => new
            {
                customerId = g.Key.UserId,
                customerName = g.Key.FullName,
                totalOrders = g.Count(),
                totalSpent = g.Sum(o => o.TotalAmount),
            })
            .OrderByDescending(x => x.totalSpent)
            .Take(topN)
            .ToListAsync();

        return Json(new { success = true, items = data });
    }
}
