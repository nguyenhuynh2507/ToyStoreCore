using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ToyStoreCore.Web.Models;

public static class HrConstants
{
    public const decimal SalaryPerCompletedShift = 200_000m;

    public const string ShiftMorning = "Sáng";
    public const string ShiftAfternoon = "Chiều";
    public const string ShiftEvening = "Tối";

    public const string StatusPending = "Chưa chấm công";
    public const string StatusCompleted = "Đã hoàn thành";
    public const string StatusAbsent = "Vắng mặt";

    public const string PayrollStatusFinalized = "Đã chốt";

    public static readonly string[] ShiftNames = { ShiftMorning, ShiftAfternoon, ShiftEvening };
    public static readonly string[] AttendanceStatuses = { StatusPending, StatusCompleted, StatusAbsent };
    public static readonly string[] UpdatableAttendanceStatuses = { StatusCompleted, StatusAbsent };
}

/// <summary>Ca làm việc của nhân viên. Ported từ ToyStore cũ (Domain/Entities/WorkShift.cs).</summary>
public class WorkShift
{
    public int Id { get; set; }

    public string StaffUserId { get; set; } = string.Empty;
    public ApplicationUser? Staff { get; set; }

    public DateTime WorkDate { get; set; }

    [StringLength(20)]
    public string ShiftName { get; set; } = string.Empty;

    [StringLength(30)]
    public string Status { get; set; } = HrConstants.StatusPending;

    [StringLength(500)]
    public string? Note { get; set; }
}

/// <summary>Bảng lương theo tháng, tính từ số ca "Đã hoàn thành". Ported từ ToyStore cũ (Domain/Entities/Payroll.cs).</summary>
public class Payroll
{
    public int Id { get; set; }

    public string StaffUserId { get; set; } = string.Empty;
    public ApplicationUser? Staff { get; set; }

    public int Month { get; set; }
    public int Year { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal BasicSalaryPerShift { get; set; }

    public int TotalShifts { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal NetSalary { get; set; }

    [StringLength(30)]
    public string Status { get; set; } = HrConstants.PayrollStatusFinalized;
}
