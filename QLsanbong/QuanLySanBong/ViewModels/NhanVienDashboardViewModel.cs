namespace QuanLySanBong.ViewModels;

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