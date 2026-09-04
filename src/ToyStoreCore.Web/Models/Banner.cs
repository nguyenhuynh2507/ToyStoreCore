using System.ComponentModel.DataAnnotations;

namespace ToyStoreCore.Web.Models;

/// <summary>
/// Banner quảng cáo trên trang chủ. Ported từ ToyStore cũ, nhưng dùng URL ảnh thay vì
/// upload file để giữ đơn giản (giống cách Product.ImageUrl đang làm trong bản rebuild này).
/// </summary>
public class Banner
{
    public int Id { get; set; }

    [StringLength(255)]
    public string? Title { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập URL ảnh banner.")]
    [StringLength(500)]
    public string ImageUrl { get; set; } = string.Empty;

    [StringLength(500)]
    public string? LinkUrl { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
