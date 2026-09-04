using System.ComponentModel.DataAnnotations;

namespace ToyStoreCore.Web.Models;

/// <summary>
/// Ghi nhận hành vi xem sản phẩm / thêm giỏ hàng của khách đã đăng nhập.
/// Dùng làm dữ liệu đầu vào cho RecommendationService (gợi ý sản phẩm kiểu Apriori).
/// Ported từ ToyStore cũ (Domain/Entities/UserActivityLog.cs + TrackActivityAttribute).
/// </summary>
public class UserActivityLog
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    /// <summary>"ViewProduct" hoặc "AddToCart" - loại hành vi được RecommendationService dùng.</summary>
    [StringLength(50)]
    public string ActivityType { get; set; } = string.Empty;

    public int? ProductId { get; set; }
    public Product? Product { get; set; }

    [StringLength(500)]
    public string? Details { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
