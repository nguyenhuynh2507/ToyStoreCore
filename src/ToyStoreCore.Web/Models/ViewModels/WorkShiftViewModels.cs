using System.ComponentModel.DataAnnotations;

namespace ToyStoreCore.Web.Models.ViewModels;

public class CreateWorkShiftViewModel
{
    [Required(ErrorMessage = "Vui lòng chọn nhân viên.")]
    public string StaffUserId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn ngày làm việc.")]
    [DataType(DataType.Date)]
    public DateTime WorkDate { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Vui lòng chọn ca làm việc.")]
    public string ShiftName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Note { get; set; }
}
