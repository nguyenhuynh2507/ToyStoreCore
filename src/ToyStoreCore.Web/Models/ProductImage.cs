using System.ComponentModel.DataAnnotations;

namespace ToyStoreCore.Web.Models;

/// <summary>
/// Ảnh phụ trong gallery sản phẩm (ngoài Product.ImageUrl là ảnh chính).
/// Ported từ ToyStore cũ (Domain/Entities/ProductImage.cs).
/// </summary>
public class ProductImage
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    [Required]
    [StringLength(500)]
    public string ImageUrl { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }
}
