using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToyStoreCore.Web.Data;
using ToyStoreCore.Web.Models;

namespace ToyStoreCore.Web.Components;

/// <summary>
/// Shows a small membership tier badge in the nav bar for logged-in customers who
/// have reached a tier. Ported from ToyStore's MembershipStatusViewComponent.
/// </summary>
public class MembershipStatusViewComponent : ViewComponent
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public MembershipStatusViewComponent(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var principal = ViewComponentContext.ViewContext.HttpContext.User;
        if (principal.Identity?.IsAuthenticated != true)
        {
            return View((MembershipTier?)null);
        }

        var userId = _userManager.GetUserId(principal);
        var user = await _db.Users.Include(u => u.Tier).FirstOrDefaultAsync(u => u.Id == userId);

        // No tier reached yet -> render nothing.
        return View(user?.Tier);
    }
}
