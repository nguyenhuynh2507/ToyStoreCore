using Microsoft.AspNetCore.Identity;

namespace ToyStoreCore.Web.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;

    // Fields ported from old ToyStore's Customer entity (Phone, Address, membership tracking).
    public string? Phone { get; set; }
    public string? Address { get; set; }

    /// <summary>Tổng số đơn hàng đã hoàn thành (giao thành công) - dùng để xét thăng hạng.</summary>
    public int TotalCompletedOrders { get; set; } = 0;

    /// <summary>Hạng thẻ thành viên hiện tại. Null nếu chưa đạt hạng nào.</summary>
    public int? TierId { get; set; }
    public MembershipTier? Tier { get; set; }

    /// <summary>Tài khoản bị khóa bởi Admin (khách vi phạm) - không cho đăng nhập khi true.</summary>
    public bool IsLocked { get; set; }
}
