using Microsoft.EntityFrameworkCore;
using ToyStoreCore.Web.Data;
using ToyStoreCore.Web.Models;

namespace ToyStoreCore.Web.Services;

/// <summary>
/// Ported from ToyStore's tier-upgrade logic in OrdersController
/// (UpdateMembershipProgressAsync) and the DiscountPercent lookup used at checkout.
/// </summary>
public class MembershipService
{
    private readonly AppDbContext _db;

    public MembershipService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>Current auto-discount percent for a user based on their tier (0 if none/guest).</summary>
    public async Task<(decimal Percent, string? TierName)> GetDiscountPercentAsync(string? userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return (0, null);
        }

        var user = await _db.Users.Include(u => u.Tier).FirstOrDefaultAsync(u => u.Id == userId);
        if (user?.Tier is null || user.Tier.DiscountPercent <= 0)
        {
            return (0, null);
        }

        return (user.Tier.DiscountPercent, $"{user.Tier.TierName} ({user.Tier.DiscountPercent:0.##}%)");
    }

    /// <summary>
    /// Increments the user's completed-order count and upgrades their tier if now qualified.
    /// Call after an order transitions into the "Completed" status (and only then, to avoid
    /// double-counting - mirrors the old system's guard on oldStatus != Completed).
    /// </summary>
    public async Task RecalculateTierAsync(string userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user is null)
        {
            return;
        }

        user.TotalCompletedOrders += 1;

        var qualifiedTier = await _db.MembershipTiers
            .Where(t => user.TotalCompletedOrders >= t.RequiredOrders)
            .OrderByDescending(t => t.RequiredOrders)
            .FirstOrDefaultAsync();

        if (qualifiedTier is not null && user.TierId != qualifiedTier.Id)
        {
            user.TierId = qualifiedTier.Id;
        }

        await _db.SaveChangesAsync();
    }
}
