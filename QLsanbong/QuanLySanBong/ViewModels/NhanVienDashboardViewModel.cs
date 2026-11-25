namespace QuanLySanBong.ViewModels;

public class StaffBookingCreateViewModel
{
    public int MaKhachHang { get; set; }
    public int MaSan { get; set; }
    public DateTime NgayDat { get; set; } = DateTime.Today;
    public TimeSpan GioBatDau { get; set; } = new(18, 0, 0);
    public TimeSpan GioKetThuc { get; set; } = new(19, 30, 0);
    public string? GhiChu { get; set; }
    public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> KhachHangOptions { get; set; } = new();
    public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> SanOptions { get; set; } = new();
}

public class NhanVienDashboardViewModel
{
    public string EmployeeName { get; set; } = "";
    public string Position { get; set; } = "Nhân viên";
    public bool HasEmployeeProfile { get; set; }
    public int ActiveFields { get; set; }
    public int BookingsToday { get; set; }
    public int PendingBookings { get; set; }
    public int CustomerCount { get; set; }
    public decimal RevenueToday { get; set; }
    public List<AdminBookingRow> RecentBookings { get; set; } = new();
    public List<NhanVienShiftRow> UpcomingShifts { get; set; } = new();
}

public class NhanVienShiftRow
{
    public DateTime Date { get; set; }
    public string Shift { get; set; } = "";
    public TimeSpan Start { get; set; }
    public TimeSpan End { get; set; }
}
