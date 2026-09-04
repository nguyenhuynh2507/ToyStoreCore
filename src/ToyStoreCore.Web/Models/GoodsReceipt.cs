using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ToyStoreCore.Web.Models;

/// <summary>
/// Phiếu nhập kho - ghi nhận mỗi lần nhập hàng, tự động cộng vào tồn kho sản phẩm.
/// Ported từ ToyStore cũ (Domain/Entities/GoodsReceipt.cs).
/// </summary>
public class GoodsReceipt
{
    public int Id { get; set; }

    public string CreatedByUserId { get; set; } = string.Empty;
    public ApplicationUser? CreatedBy { get; set; }

    public DateTime ImportDate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalCost { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }

    public ICollection<GoodsReceiptDetail> Details { get; set; } = new List<GoodsReceiptDetail>();
}

public class GoodsReceiptDetail
{
    public int Id { get; set; }

    public int GoodsReceiptId { get; set; }
    public GoodsReceipt? GoodsReceipt { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Range(0.01, double.MaxValue)]
    public decimal ImportPrice { get; set; }
}
