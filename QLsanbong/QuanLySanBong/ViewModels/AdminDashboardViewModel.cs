namespace QuanLySanBong.ViewModels;

public class AdminDashboardViewModel
{
    public decimal RevenueToday { get; set; }
    public int BookingsToday { get; set; }
    public int ActiveFields { get; set; }
    public int TotalFields { get; set; }
    public int PendingBookings { get; set; }
    public int FillRate { get; set; }
    public int CustomerCount { get; set; }
    public int EmployeeCount { get; set; }
    public int ServiceCount { get; set; }
    public int InvoiceCount { get; set; }
    public int FieldTypeCount { get; set; }
    public int PriceCount { get; set; }
    public int PaymentTypeCount { get; set; }
    public int ShiftCount { get; set; }
    public int ScheduleCount { get; set; }
    public int ReviewCount { get; set; }
    public int NotificationCount { get; set; }
    public int SupportCount { get; set; }
    public int UserCount { get; set; }
    public int RoleCount { get; set; }
    public int LogCount { get; set; }
    public List<AdminBookingRow> RecentBookings { get; set; } = new();
    public List<AdminRevenuePoint> MonthlyRevenue { get; set; } = new();
}

public class AdminBookingRow
{
    public int Id { get; set; }
    public string Customer { get; set; } = string.Empty;
    public string Field { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public TimeSpan Start { get; set; }
    public TimeSpan End { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class AdminRevenuePoint
{
    public string Month { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
