using System.ComponentModel.DataAnnotations;

namespace ToyStoreCore.Web.Models.ViewModels;

public class CreateStaffViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
    [StringLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [StringLength(20)]
    public string? Phone { get; set; }
}

public class EditStaffViewModel
{
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
    [StringLength(150)]
    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    [StringLength(20)]
    public string? Phone { get; set; }

    // Chỉ đổi mật khẩu nếu nhập giá trị mới.
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự.")]
    [DataType(DataType.Password)]
    public string? NewPassword { get; set; }
}
