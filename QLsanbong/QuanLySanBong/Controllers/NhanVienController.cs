using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLySanBong.Data;
using QuanLySanBong.ViewModels;

namespace QuanLySanBong.Controllers;

[Authorize(Roles = "Nhân viên,Nhân viên thu ngân")]
public class NhanVienController : Controller
{
    private readonly AppDbContext _db;
    public NhanVienController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        var accountId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
        var account = await _db.NguoiDungs.AsNoTracking().FirstOrDefaultAsync(x => x.MaNguoiDung == accountId);
        var name = User.Identity?.Name ?? "Nhân viên";
        var employee = await _db.NhanViens.AsNoTracking().FirstOrDefaultAsync(x =>
            x.HoTen == name || (account != null && account.Email != null && x.Email == account.Email));

        var model = new NhanVienDashboardViewModel
        {
            EmployeeName = name,
            Position = employee?.ChucVu ?? "Nhân viên",
            HasEmployeeProfile = employee != null,
            ActiveFields = await _db.SanBongs.CountAsync(x => x.TrangThai == "Hoạt động"),
            BookingsToday = await _db.PhieuDatSans.CountAsync(x => x.NgayDat >= today && x.NgayDat < tomorrow),
            PendingBookings = await _db.PhieuDatSans.CountAsync(x => x.TrangThai == "Chờ xác nhận"),
            CustomerCount = await _db.KhachHangs.CountAsync(),
            RevenueToday = await _db.HoaDons.Where(x => x.NgayLap >= today && x.NgayLap < tomorrow && x.TrangThai)
                .SumAsync(x => (decimal?)x.TongCong) ?? 0m,
            RecentBookings = await _db.PhieuDatSans.AsNoTracking().Include(x => x.KhachHang).Include(x => x.SanBong)
                .OrderByDescending(x => x.NgayTao).Take(8).Select(x => new AdminBookingRow
                {
                    Id = x.MaPhieu, Customer = x.KhachHang != null ? x.KhachHang.HoTen : "Khách vãng lai",
                    Field = x.SanBong != null ? x.SanBong.TenSan : "—", Date = x.NgayDat,
                    Start = x.GioBatDau, End = x.GioKetThuc, Amount = x.TongTienSan, Status = x.TrangThai
                }).ToListAsync(),
            UpcomingShifts = employee == null ? new List<NhanVienShiftRow>() : await _db.LichLams.AsNoTracking()
                .Where(x => x.MaNhanVien == employee.MaNhanVien && x.TrangThai && x.NgayLam >= today)
                .Include(x => x.CaLamViec).OrderBy(x => x.NgayLam).Take(6).Select(x => new NhanVienShiftRow
                {
                    Date = x.NgayLam, Shift = x.CaLamViec != null ? x.CaLamViec.TenCa : "Ca làm",
                    Start = x.CaLamViec != null ? x.CaLamViec.ThoiGianBatDau : TimeSpan.Zero,
                    End = x.CaLamViec != null ? x.CaLamViec.ThoiGianKetThuc : TimeSpan.Zero
                }).ToListAsync()
        };
        await SetSidebarCounts(employee?.MaNhanVien, accountId);
        ViewBag.ActiveModule = "dashboard";
        ViewData["Title"] = "Tổng quan nhân viên";
        return View(model);
    }

    private async Task SetSidebarCounts(int? employeeId, int accountId)
    {
        ViewBag.FieldCount = await _db.SanBongs.CountAsync();
        ViewBag.PriceCount = await _db.BangGias.CountAsync();
        ViewBag.CustomerCount = await _db.KhachHangs.CountAsync();
        ViewBag.PendingCount = await _db.PhieuDatSans.CountAsync(x => x.TrangThai == "Chờ xác nhận");
        ViewBag.ServiceCount = await _db.DichVus.CountAsync();
        ViewBag.InvoiceCount = await _db.HoaDons.CountAsync();
        ViewBag.InvoiceDetailCount = await _db.ChiTietHoaDons.CountAsync();
        ViewBag.ScheduleCount = employeeId == null ? 0 : await _db.LichLams.CountAsync(x => x.MaNhanVien == employeeId);
        ViewBag.ReviewCount = await _db.DanhGias.CountAsync();
        ViewBag.NotificationCount = await _db.ThongBaos.CountAsync(x => x.MaNguoiDung == accountId);
        ViewBag.SupportCount = await _db.HoTros.CountAsync();
    }
}