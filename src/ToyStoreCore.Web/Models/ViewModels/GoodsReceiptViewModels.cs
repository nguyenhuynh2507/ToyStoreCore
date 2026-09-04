using System.ComponentModel.DataAnnotations;

namespace ToyStoreCore.Web.Models.ViewModels;

public class GoodsReceiptLineViewModel
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal ImportPrice { get; set; }
}

public class CreateGoodsReceiptViewModel
{
    [DataType(DataType.Date)]
    public DateTime ImportDate { get; set; } = DateTime.Today;

    [StringLength(500)]
    public string? Note { get; set; }

    public List<GoodsReceiptLineViewModel> Details { get; set; } = new();
}
