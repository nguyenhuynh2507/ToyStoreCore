using ToyStoreCore.Web.Data;
using ToyStoreCore.Web.Models;

namespace ToyStoreCore.Web.Services;

/// <summary>
/// Records logged-in customer behavior (product views, add-to-cart) for the recommendation
/// engine. Ported from ToyStore's TrackActivityAttribute - logging failures must never break
/// the request, so every call swallows its own exceptions.
/// </summary>
public class ActivityTrackingService
{
    private const int MaxDetailsLength = 500;

    private readonly AppDbContext _db;
    private readonly ILogger<ActivityTrackingService> _logger;

    public ActivityTrackingService(AppDbContext db, ILogger<ActivityTrackingService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task TrackAsync(string? userId, string activityType, int? productId = null, string? details = null)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return; // only logged-in customers are tracked, same as the old system
        }

        try
        {
            _db.UserActivityLogs.Add(new UserActivityLog
            {
                UserId = userId,
                ActivityType = activityType,
                ProductId = productId,
                Details = details?.Length > MaxDetailsLength ? details[..MaxDetailsLength] : details,
            });

            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể ghi UserActivityLog cho {ActivityType}", activityType);
        }
    }
}
