using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ToyStoreCore.Web.Models;

public class Product
{
    public int Id { get; set; }

    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }

    [StringLength(500)]
    public string? ImageUrl { get; set; }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
}
