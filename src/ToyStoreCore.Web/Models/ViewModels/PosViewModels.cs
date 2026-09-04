namespace ToyStoreCore.Web.Models.ViewModels;

public class PosOrderItemModel
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class PosOrderModel
{
    public List<PosOrderItemModel> Items { get; set; } = new();
    public string PaymentMethod { get; set; } = string.Empty;
    public string? CustomerId { get; set; }
}
