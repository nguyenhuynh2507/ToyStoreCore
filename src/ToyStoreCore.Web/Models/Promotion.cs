using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ToyStoreCore.Web.Models;

public enum PromotionDiscountType
{
    Percentage = 0,
    FixedAmount = 1,
}

/// <summary>
/// Mã khuyến mãi (voucher) khách nhập ở giỏ hàng/checkout.
/// Ported từ ToyStore cũ (Domain/Entities/Promotion.cs), logic của SP_APPLY_PROMOTION
/// được viết lại thuần C# trong PromotionService thay vì Oracle stored procedure.
/// </summary>
public class Promotion
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Mã code là bắt buộc")]
    [StringLength(50)]
    public string PromotionCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tên chương trình là bắt buộc")]
    [StringLength(255)]
    public string PromotionName { get; set; } = string.Empty;

    [Required]
    public PromotionDiscountType DiscountType { get; set; }

    [Range(0, double.MaxValue)]
    [Column(TypeName = "decimal(18,2)")]
    public decimal DiscountValue { get; set; }

    [Range(0, double.MaxValue)]
    [Column(TypeName = "decimal(18,2)")]
    public decimal MinOrderValue { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    /// <summary>0 = không giới hạn lượt dùng.</summary>
    [Range(0, int.MaxValue)]
    public int UsageLimit { get; set; }

    [Range(0, int.MaxValue)]
    public int UsedCount { get; set; }

    public bool IsActive { get; set; } = true;
}
