using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToyStoreCore.Web.Data;
using ToyStoreCore.Web.Models;

namespace ToyStoreCore.Web.Controllers.Admin;

[Authorize(Roles = IdentitySeeder.AdminRole)]
[Route("Admin/Payroll")]
public class AdminPayrollController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AdminPayrollController> _logger;

    public AdminPayrollController(AppDbContext db, UserManager<ApplicationUser> userManager, ILogger<AdminPayrollController> logger)
    {
        _db = db;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(int? month, int? year)
    {
        var filterMonth = month is >= 1 and <= 12 ? month.Value : DateTime.Now.Month;
        var filterYear = year is >= 2000 and <= 2100 ? year.Value : DateTime.Now.Year;

        var payrolls = await _db.Payrolls
            .AsNoTracking()
            .Include(p => p.Staff)
            .Where(p => p.Month == filterMonth && p.Year == filterYear)
            .OrderBy(p => p.Staff!.FullName)
            .ToListAsync();

        ViewBag.Month = filterMonth;
        ViewBag.Year = filterYear;
        ViewBag.TotalNetSalary = payrolls.Sum(p => p.NetSalary);
        ViewBag.TotalShifts = payrolls.Sum(p => p.TotalShifts);

        return View(payrolls);
    }

    [HttpPost("Calculate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Calculate(int month, int year)
    {
        if (month is < 1 or > 12 || year is < 2000 or > 2100)
        {
            TempData["Error"] = "Tháng/năm không hợp lệ.";
            return RedirectToAction(nameof(Index));
        }

        var periodStart = new DateTime(year, month, 1);
        var periodEnd = periodStart.AddMonths(1);

        try
        {
            var staffUsers = await _userManager.GetUsersInRoleAsync(IdentitySeeder.StaffRole);
            if (staffUsers.Count == 0)
            {
                TempData["Error"] = "Không có nhân viên để tính lương.";
                return RedirectToAction(nameof(Index), new { month, year });
            }

            var shiftCounts = await _db.WorkShifts
                .AsNoTracking()
                .Where(w => w.Status == HrConstants.StatusCompleted && w.WorkDate >= periodStart && w.WorkDate < periodEnd)
                .GroupBy(w => w.StaffUserId)
                .Select(g => new { StaffUserId = g.Key, Count = g.Count() })
                .ToListAsync();

            var countLookup = shiftCounts.ToDictionary(x => x.StaffUserId, x => x.Count);

            var existingPayrolls = await _db.Payrolls
                .Where(p => p.Month == month && p.Year == year)
                .ToListAsync();
            var payrollLookup = existingPayrolls.ToDictionary(p => p.StaffUserId);

            var created = 0;
            var updated = 0;

            foreach (var staff in staffUsers)
            {
                countLookup.TryGetValue(staff.Id, out var completedCount);
                var basicSalary = HrConstants.SalaryPerCompletedShift;
                var netSalary = completedCount * basicSalary;

                if (payrollLookup.TryGetValue(staff.Id, out var existing))
                {
                    existing.TotalShifts = completedCount;
                    existing.BasicSalaryPerShift = basicSalary;
                    existing.NetSalary = netSalary;
                    existing.Status = HrConstants.PayrollStatusFinalized;
                    updated++;
                }
                else
                {
                    _db.Payrolls.Add(new Payroll
                    {
                        StaffUserId = staff.Id,
                        Month = month,
                        Year = year,
                        TotalShifts = completedCount,
                        BasicSalaryPerShift = basicSalary,
                        NetSalary = netSalary,
                        Status = HrConstants.PayrollStatusFinalized,
                    });
                    created++;
                }
            }

            await _db.SaveChangesAsync();
            TempData["Message"] = $"Chốt lương tháng {month}/{year} thành công: {created} bản ghi mới, {updated} bản ghi cập nhật.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi chốt lương tháng {Month}/{Year}", month, year);
            TempData["Error"] = "Không thể chốt lương. Vui lòng kiểm tra dữ liệu ca làm việc và thử lại.";
        }

        return RedirectToAction(nameof(Index), new { month, year });
    }
}
