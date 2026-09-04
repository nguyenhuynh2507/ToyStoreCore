using System.ComponentModel.DataAnnotations;

namespace ToyStoreCore.Web.Models.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
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

    [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [StringLength(20)]
    public string? Phone { get; set; }

    [StringLength(300)]
    public string? Address { get; set; }

    public string? ReturnUrl { get; set; }
}

public class ProfileViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
    [StringLength(150)]
    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    [StringLength(20)]
    public string? Phone { get; set; }

    [StringLength(300)]
    public string? Address { get; set; }

    // Display-only membership info, not editable here.
    public string? TierName { get; set; }
    public decimal TierDiscountPercent { get; set; }
    public int TotalCompletedOrders { get; set; }
    public int? NextTierRequiredOrders { get; set; }
    public string? NextTierName { get; set; }
}
