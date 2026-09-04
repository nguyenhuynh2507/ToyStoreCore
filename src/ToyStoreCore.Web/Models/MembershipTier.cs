using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ToyStoreCore.Web.Models;

/// <summary>
/// Hạng thành viên - xếp hạng khách hàng theo số đơn đã hoàn thành,
/// áp dụng mức giảm giá tương ứng tự động ở checkout.
/// Ported từ ToyStore cũ (Domain/Entities/MembershipTier.cs).
/// </summary>
public class MembershipTier
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Tên hạng thẻ là bắt buộc")]
    [StringLength(100)]
    public string TierName { get; set; } = string.Empty;

    /// <summary>Số đơn hàng hoàn thành tối thiểu để đạt hạng thẻ này.</summary>
    [Range(0, int.MaxValue)]
    public int RequiredOrders { get; set; }

    /// <summary>Phần trăm giảm giá tự động áp dụng ở checkout (ví dụ 10 = giảm 10%).</summary>
    [Range(0, 100)]
    [Column(TypeName = "decimal(5,2)")]
    public decimal DiscountPercent { get; set; }
}
