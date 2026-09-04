using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ToyStoreCore.Web.Models;

public enum OrderStatus
{
    Pending = 0,
    Confirmed = 1,
    Shipping = 2,
    Completed = 3,
    Cancelled = 4
}

public class Order
{
    public int Id { get; set; }

    // Null for guest checkout; set when a logged-in customer places the order.
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    [Required, StringLength(150)]
    public string CustomerName { get; set; } = string.Empty;

    [Required, StringLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required, StringLength(300)]
    public string ShippingAddress { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Note { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Subtotal { get; set; }

    /// <summary>Giảm giá tự động theo hạng thành viên tại thời điểm đặt hàng.</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal MembershipDiscount { get; set; }

    [StringLength(50)]
    public string? PromoCode { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal PromoDiscount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    /// <summary>"Online" (mặc định) hoặc "POS" (bán tại quầy).</summary>
    [StringLength(20)]
    public string OrderType { get; set; } = "Online";

    /// <summary>Chỉ dùng cho đơn POS: "Tiền mặt" hoặc "Chuyển khoản (QR)". Null cho đơn online.</summary>
    [StringLength(50)]
    public string? PaymentMethod { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}

public class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order? Order { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    [StringLength(200)]
    public string ProductName { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
}
