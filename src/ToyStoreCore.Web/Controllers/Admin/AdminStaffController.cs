using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToyStoreCore.Web.Data;
using ToyStoreCore.Web.Models;
using ToyStoreCore.Web.Models.ViewModels;

namespace ToyStoreCore.Web.Controllers.Admin;

[Authorize(Roles = IdentitySeeder.AdminRole)]
[Route("Admin/Staff")]
public class AdminStaffController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminStaffController(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var staffUsers = await _userManager.GetUsersInRoleAsync(IdentitySeeder.StaffRole);
        return View(staffUsers.OrderBy(s => s.FullName).ToList());
    }

    [HttpGet("Details/{id}")]
    public async Task<IActionResult> Details(string id)
    {
        var staff = await _db.Users.FindAsync(id);
        if (staff is null)
        {
            return NotFound();
        }

        return View(staff);
    }

    [HttpGet("Create")]
    public IActionResult Create()
    {
        return View(new CreateStaffViewModel());
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateStaffViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            Phone = model.Phone,
            EmailConfirmed = true,
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        await _userManager.AddToRoleAsync(user, IdentitySeeder.StaffRole);

        TempData["Message"] = "Đã tạo tài khoản nhân viên.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Edit/{id}")]
    public async Task<IActionResult> Edit(string id)
    {
        var staff = await _db.Users.FindAsync(id);
        if (staff is null)
        {
            return NotFound();
        }

        return View(new EditStaffViewModel
        {
            Id = staff.Id,
            FullName = staff.FullName,
            Email = staff.Email ?? string.Empty,
            Phone = staff.Phone,
        });
    }

    [HttpPost("Edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, EditStaffViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var staff = await _userManager.FindByIdAsync(id);
        if (staff is null)
        {
            return NotFound();
        }

        staff.FullName = model.FullName;
        staff.Phone = model.Phone;
        await _userManager.UpdateAsync(staff);

        if (!string.IsNullOrWhiteSpace(model.NewPassword))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(staff);
            var resetResult = await _userManager.ResetPasswordAsync(staff, token, model.NewPassword);
            if (!resetResult.Succeeded)
            {
                foreach (var error in resetResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }
        }

        TempData["Message"] = "Đã cập nhật thông tin nhân viên.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Delete/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var staff = await _userManager.FindByIdAsync(id);
        if (staff is not null)
        {
            await _userManager.DeleteAsync(staff);
        }

        TempData["Message"] = "Đã xóa tài khoản nhân viên.";
        return RedirectToAction(nameof(Index));
    }
}
