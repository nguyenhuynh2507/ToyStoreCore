using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ToyStoreCore.Web.Data;
using ToyStoreCore.Web.Models;
using ToyStoreCore.Web.Models.ViewModels;

namespace ToyStoreCore.Web.Controllers.Admin;

[Authorize(Roles = IdentitySeeder.AdminRole)]
[Route("Admin/WorkShift")]
public class AdminWorkShiftController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AdminWorkShiftController> _logger;

    public AdminWorkShiftController(AppDbContext db, UserManager<ApplicationUser> userManager, ILogger<AdminWorkShiftController> logger)
    {
        _db = db;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? staffId, int? month, int? year, string? status)
    {
        var filterMonth = month is >= 1 and <= 12 ? month.Value : DateTime.Now.Month;
        var filterYear = year is >= 2000 and <= 2100 ? year.Value : DateTime.Now.Year;
        var periodStart = new DateTime(filterYear, filterMonth, 1);
        var periodEnd = periodStart.AddMonths(1);

        var query = _db.WorkShifts.Include(w => w.Staff)
            .Where(w => w.WorkDate >= periodStart && w.WorkDate < periodEnd);

        if (!string.IsNullOrEmpty(staffId))
        {
            query = query.Where(w => w.StaffUserId == staffId);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(w => w.Status == status);
        }

        var shifts = await query.OrderByDescending(w => w.WorkDate).ThenBy(w => w.ShiftName).ToListAsync();

        await PopulateStaffDropdownAsync(staffId);
        ViewBag.Month = filterMonth;
        ViewBag.Year = filterYear;
        ViewBag.Status = status;
        ViewBag.StatusOptions = HrConstants.AttendanceStatuses;

        return View(shifts);
    }

    [HttpGet("Create")]
    public async Task<IActionResult> Create()
    {
        await PopulateStaffDropdownAsync(null);
        return View(new CreateWorkShiftViewModel());
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateWorkShiftViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateStaffDropdownAsync(model.StaffUserId);
            return View(model);
        }

        if (!HrConstants.ShiftNames.Contains(model.ShiftName))
        {
            ModelState.AddModelError(nameof(model.ShiftName), "Ca làm việc không hợp lệ.");
            await PopulateStaffDropdownAsync(model.StaffUserId);
            return View(model);
        }

        var staff = await _userManager.FindByIdAsync(model.StaffUserId);
        if (staff is null || !await _userManager.IsInRoleAsync(staff, IdentitySeeder.StaffRole))
        {
            ModelState.AddModelError(nameof(model.StaffUserId), "Nhân viên không tồn tại hoặc không hợp lệ.");
            await PopulateStaffDropdownAsync(model.StaffUserId);
            return View(model);
        }

        var workDate = model.WorkDate.Date;
        var duplicate = await _db.WorkShifts.AnyAsync(w =>
            w.StaffUserId == model.StaffUserId && w.WorkDate == workDate && w.ShiftName == model.ShiftName);

        if (duplicate)
        {
            ModelState.AddModelError(string.Empty, $"Nhân viên đã có ca {model.ShiftName} vào ngày {workDate:dd/MM/yyyy}.");
            await PopulateStaffDropdownAsync(model.StaffUserId);
            return View(model);
        }

        try
        {
            _db.WorkShifts.Add(new WorkShift
            {
                StaffUserId = model.StaffUserId,
                WorkDate = workDate,
                ShiftName = model.ShiftName,
                Status = HrConstants.StatusPending,
                Note = model.Note,
            });

            await _db.SaveChangesAsync();
            TempData["Message"] = "Phân ca làm việc thành công.";
            return RedirectToAction(nameof(Index), new { staffId = model.StaffUserId, month = workDate.Month, year = workDate.Year });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tạo ca làm việc cho StaffUserId={StaffUserId}", model.StaffUserId);
            ModelState.AddModelError(string.Empty, "Không thể lưu ca làm việc. Vui lòng thử lại.");
            await PopulateStaffDropdownAsync(model.StaffUserId);
            return View(model);
        }
    }

    [HttpPost("UpdateStatus")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int shiftId, string status, int? month, int? year, string? staffId, string? filterStatus)
    {
        if (!HrConstants.UpdatableAttendanceStatuses.Contains(status))
        {
            TempData["Error"] = "Trạng thái chấm công không hợp lệ.";
            return RedirectToAction(nameof(Index), new { staffId, month, year, status = filterStatus });
        }

        var shift = await _db.WorkShifts.FindAsync(shiftId);
        if (shift is null)
        {
            TempData["Error"] = "Không tìm thấy ca làm việc.";
            return RedirectToAction(nameof(Index), new { staffId, month, year, status = filterStatus });
        }

        shift.Status = status;
        await _db.SaveChangesAsync();
        TempData["Message"] = $"Đã cập nhật chấm công ca #{shiftId} thành \"{status}\".";

        return RedirectToAction(nameof(Index), new { staffId, month = month ?? shift.WorkDate.Month, year = year ?? shift.WorkDate.Year, status = filterStatus });
    }

    private async Task PopulateStaffDropdownAsync(string? selectedStaffId)
    {
        var staffUsers = await _userManager.GetUsersInRoleAsync(IdentitySeeder.StaffRole);
        var ordered = staffUsers.OrderBy(s => s.FullName).ToList();

        ViewBag.StaffList = new SelectList(ordered, nameof(ApplicationUser.Id), nameof(ApplicationUser.FullName), selectedStaffId);
        ViewBag.ShiftNames = HrConstants.ShiftNames;
    }
}
