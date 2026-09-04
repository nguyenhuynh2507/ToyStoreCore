namespace ToyStoreCore.Web.Models.ViewModels;

public class ProductDetailsViewModel
{
    public Product Product { get; set; } = null!;
    public List<Product> RecommendedProducts { get; set; } = new();
}
