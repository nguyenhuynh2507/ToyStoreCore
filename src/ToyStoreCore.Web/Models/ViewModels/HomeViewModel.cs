namespace ToyStoreCore.Web.Models.ViewModels;

public class HomeViewModel
{
    public List<Product> FeaturedProducts { get; set; } = new();
    public List<Banner> Banners { get; set; } = new();
}
