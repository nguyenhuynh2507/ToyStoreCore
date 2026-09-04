using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToyStoreCore.Web.Data;
using ToyStoreCore.Web.Models;
using ToyStoreCore.Web.Models.ViewModels;
using ToyStoreCore.Web.Services;

namespace ToyStoreCore.Web.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly AppDbContext _db;
    private readonly CartService _cartService;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        AppDbContext db,
        CartService cartService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _db = db;
        _cartService = cartService;
    }

    [HttpGet]
    public IActionResult Register(string? returnUrl = null)
    {
        return View(new RegisterViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
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
            Address = model.Address,
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

        await _userManager.AddToRoleAsync(user, IdentitySeeder.CustomerRole);
        await _signInManager.SignInAsync(user, isPersistent: false);
        await _cartService.MergeGuestCartIntoUserCartOnLoginAsync(user.Id);

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var existingUser = await _userManager.FindByEmailAsync(model.Email);
        if (existingUser is not null && existingUser.IsLocked)
        {
            ModelState.AddModelError(string.Empty, "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng.");
            return View(model);
        }

        await _cartService.MergeGuestCartIntoUserCartOnLoginAsync(existingUser!.Id);

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var user = await _db.Users.Include(u => u.Tier).FirstAsync(u => u.Id == _userManager.GetUserId(User));

        var model = new ProfileViewModel
        {
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            Phone = user.Phone,
            Address = user.Address,
            TierName = user.Tier?.TierName,
            TierDiscountPercent = user.Tier?.DiscountPercent ?? 0,
            TotalCompletedOrders = user.TotalCompletedOrders,
        };

        var nextTier = await _db.MembershipTiers
            .Where(t => t.RequiredOrders > user.TotalCompletedOrders)
            .OrderBy(t => t.RequiredOrders)
            .FirstOrDefaultAsync();

        if (nextTier is not null)
        {
            model.NextTierName = nextTier.TierName;
            model.NextTierRequiredOrders = nextTier.RequiredOrders;
        }

        return View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        var user = await _db.Users.Include(u => u.Tier).FirstAsync(u => u.Id == _userManager.GetUserId(User));

        // Email is display-only here (changing it would require re-confirming/re-verifying).
        model.Email = user.Email ?? string.Empty;
        model.TierName = user.Tier?.TierName;
        model.TierDiscountPercent = user.Tier?.DiscountPercent ?? 0;
        model.TotalCompletedOrders = user.TotalCompletedOrders;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        user.FullName = model.FullName;
        user.Phone = model.Phone;
        user.Address = model.Address;
        await _db.SaveChangesAsync();

        TempData["Message"] = "Đã cập nhật thông tin cá nhân.";
        return RedirectToAction(nameof(Profile));
    }
}
