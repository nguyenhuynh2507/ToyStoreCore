using System.ComponentModel.DataAnnotations;

namespace ToyStoreCore.Web.Models.ViewModels;

// Stored serialized as JSON inside the session - not an EF entity.
public class CartLine
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public string? ImageUrl { get; set; }
    public int Quantity { get; set; }

    public decimal LineTotal => UnitPrice * Quantity;
}

public class CartViewModel
{
    public List<CartLine> Lines { get; set; } = new();
    public decimal Total => Lines.Sum(l => l.LineTotal);

    // Voucher applied at the cart/checkout step (ported from ToyStore's PromoSessionHelper).
    public string? AppliedPromoCode { get; set; }
    public decimal AppliedPromoDiscount { get; set; }

    public decimal FinalTotal
    {
        get
        {
            var discount = AppliedPromoDiscount;
            if (discount < 0) discount = 0;
            if (discount > Total) discount = Total;
            return Total - discount;
        }
    }
}

public class CheckoutViewModel
{
    public CartViewModel Cart { get; set; } = new();

    // "Mua ngay" từ trang chi tiết sản phẩm - bỏ qua giỏ hàng, không cho áp mã khuyến mãi
    // (để đơn giản hóa flow, giống hành vi thường thấy ở các site TMĐT).
    public bool IsQuickBuy { get; set; }

    // Khi khách chỉ chọn một phần giỏ hàng để thanh toán (không phải toàn bộ).
    // Giữ lại qua các lần postback bằng hidden input.
    public List<int> SelectedProductIds { get; set; } = new();

    // Read-only, computed for display: membership discount stacks with the voucher.
    // Ported from ToyStore's CheckoutPricingHelper (Subtotal - MembershipDiscount - Voucher).
    public decimal MembershipDiscountPercent { get; set; }
    public decimal MembershipDiscountValue { get; set; }
    public string? MembershipTierLabel { get; set; }

    public decimal FinalTotal
    {
        get
        {
            var total = Cart.FinalTotal - MembershipDiscountValue;
            return total < 0 ? 0 : total;
        }
    }

    [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
    [StringLength(150)]
    public string CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
    [StringLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng.")]
    [StringLength(300)]
    public string ShippingAddress { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Note { get; set; }
}
