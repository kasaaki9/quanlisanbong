using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLySanBong.Data;
using QuanLySanBong.Models;
using QuanLySanBong.ViewModels;

namespace QuanLySanBong.Controllers;

[Authorize(Roles = "Nhân viên,Nhân viên thu ngân")]
public class NhanVienController : Controller
{
    private static readonly TimeSpan BookingDuration = TimeSpan.FromMinutes(90);
    private readonly AppDbContext _db;
    public NhanVienController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        var accountId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
        var account = await _db.NguoiDungs.AsNoTracking().FirstOrDefaultAsync(x => x.MaNguoiDung == accountId);
        if (account == null || !account.TrangThai)
        {
            await HttpContext.SignOutAsync("Cookies");
            return RedirectToAction("Login", "Account");
        }

        var name = account.HoTen;
        NhanVien? employee = null;
        if (!string.IsNullOrWhiteSpace(account.Email))
            employee = await _db.NhanViens.AsNoTracking().FirstOrDefaultAsync(x => x.Email == account.Email);
        if (employee == null && !string.IsNullOrWhiteSpace(account.SoDienThoai))
            employee = await _db.NhanViens.AsNoTracking().FirstOrDefaultAsync(x => x.SoDienThoai == account.SoDienThoai);
        if (employee == null)
            employee = await _db.NhanViens.AsNoTracking().FirstOrDefaultAsync(x => x.HoTen == account.HoTen);

        if (employee is { TrangThai: false })
        {
            await HttpContext.SignOutAsync("Cookies");
            return RedirectToAction("Login", "Account");
        }

        var currentTime = DateTime.Now.TimeOfDay;
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
                .Where(x => x.MaNhanVien == employee.MaNhanVien && x.TrangThai &&
                    (x.NgayLam >= tomorrow || (x.NgayLam >= today && x.NgayLam < tomorrow &&
                    (x.CaLamViec == null || x.CaLamViec.ThoiGianKetThuc > currentTime))))
                .Include(x => x.CaLamViec).OrderBy(x => x.NgayLam)
                .ThenBy(x => x.CaLamViec == null ? TimeSpan.Zero : x.CaLamViec.ThoiGianBatDau).Take(6).Select(x => new NhanVienShiftRow
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

    [HttpGet]
    public async Task<IActionResult> TaoPhieuDatSan()
    {
        var (account, employee) = await LoadStaffContextAsync();
        if (account == null || employee is { TrangThai: false })
        {
            await HttpContext.SignOutAsync("Cookies");
            return RedirectToAction("Login", "Account");
        }
        if (employee == null)
        {
            TempData["AdminError"] = "Tài khoản chưa được liên kết với hồ sơ nhân viên.";
            return RedirectToAction(nameof(Index));
        }

        var model = new StaffBookingCreateViewModel();
        await FillBookingOptionsAsync(model);
        await SetSidebarCounts(employee.MaNhanVien, account.MaNguoiDung);
        ViewBag.ActiveModule = "phieu-dat-san";
        ViewData["Title"] = "Tạo phiếu đặt sân";
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TaoPhieuDatSan(StaffBookingCreateViewModel model)
    {
        var (account, employee) = await LoadStaffContextAsync();
        if (account == null || employee is { TrangThai: false })
        {
            await HttpContext.SignOutAsync("Cookies");
            return RedirectToAction("Login", "Account");
        }
        if (employee == null)
        {
            TempData["AdminError"] = "Tài khoản chưa được liên kết với hồ sơ nhân viên.";
            return RedirectToAction(nameof(Index));
        }

        async Task<IActionResult> RebuildViewAsync()
        {
            await FillBookingOptionsAsync(model);
            await SetSidebarCounts(employee.MaNhanVien, account.MaNguoiDung);
            ViewBag.ActiveModule = "phieu-dat-san";
            ViewData["Title"] = "Tạo phiếu đặt sân";
            return View(model);
        }

        if (!ModelState.IsValid)
            return await RebuildViewAsync();

        var bookingDate = model.NgayDat.Date;
        if (bookingDate < DateTime.Today || (bookingDate == DateTime.Today && model.GioBatDau <= DateTime.Now.TimeOfDay))
            ModelState.AddModelError(string.Empty, "Thời gian đặt sân phải ở hiện tại hoặc trong tương lai.");
        if (model.GioKetThuc - model.GioBatDau != BookingDuration)
            ModelState.AddModelError(string.Empty, "Mỗi phiếu đặt sân có thời lượng 90 phút.");
        if (model.GioBatDau < TimeSpan.Zero || model.GioKetThuc >= TimeSpan.FromDays(1))
            ModelState.AddModelError(string.Empty, "Khung giờ không hợp lệ.");

        var customer = await _db.KhachHangs.FirstOrDefaultAsync(x =>
            x.MaKhachHang == model.MaKhachHang && x.TrangThai);
        if (customer == null)
            ModelState.AddModelError(nameof(model.MaKhachHang), "Vui lòng chọn khách hàng đang hoạt động.");

        var field = await _db.SanBongs.FirstOrDefaultAsync(x =>
            x.MaSan == model.MaSan && x.TrangThai == "Hoạt động");
        if (field == null)
            ModelState.AddModelError(nameof(model.MaSan), "Vui lòng chọn sân đang hoạt động.");

        BangGia? rate = null;
        if (field != null && model.GioKetThuc - model.GioBatDau == BookingDuration)
        {
            var dayType = bookingDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ? "Cuối tuần" : "Ngày thường";
            var rates = await _db.BangGias.AsNoTracking().Where(x => x.MaSan == field.MaSan).ToListAsync();
            rate = rates.Where(x => x.GioBatDau <= model.GioBatDau && x.GioKetThuc >= model.GioKetThuc &&
                    (string.IsNullOrWhiteSpace(x.LoaiNgay) || x.LoaiNgay == "Tất cả" || x.LoaiNgay == dayType))
                .OrderByDescending(x => x.LoaiNgay == dayType).FirstOrDefault();

            if (rate == null)
                ModelState.AddModelError(string.Empty, "Không có bảng giá áp dụng cho sân và khung giờ đã chọn.");
            else if ((model.GioBatDau - rate.GioBatDau).Ticks % BookingDuration.Ticks != 0)
                ModelState.AddModelError(string.Empty, "Giờ bắt đầu cần nằm đúng khung 90 phút theo bảng giá.");

            var hasConflict = await _db.PhieuDatSans.AnyAsync(x => x.MaSan == field.MaSan &&
                x.NgayDat == bookingDate && !x.TrangThai.Contains("hủy") &&
                x.GioBatDau < model.GioKetThuc && x.GioKetThuc > model.GioBatDau);
            if (hasConflict)
                ModelState.AddModelError(string.Empty, "Sân đã có phiếu đặt trùng khung giờ.");
        }

        if (!ModelState.IsValid || customer == null || field == null || rate == null)
            return await RebuildViewAsync();

        var booking = new PhieuDatSan
        {
            MaKhachHang = customer.MaKhachHang,
            MaSan = field.MaSan,
            NgayDat = bookingDate,
            GioBatDau = model.GioBatDau,
            GioKetThuc = model.GioKetThuc,
            TongTienSan = decimal.Round(rate.GiaTien * (decimal)BookingDuration.TotalHours, 0),
            TrangThai = "Đã xác nhận",
            GhiChu = string.IsNullOrWhiteSpace(model.GhiChu)
                ? "Được nhân viên tạo trực tiếp."
                : $"Được nhân viên tạo trực tiếp. {model.GhiChu.Trim()}",
            NgayTao = DateTime.Now
        };
        _db.PhieuDatSans.Add(booking);
        await _db.SaveChangesAsync();
        TempData["AdminSuccess"] = $"Đã tạo và xác nhận phiếu #{booking.MaPhieu} với tiền sân {booking.TongTienSan:N0} đ.";
        return RedirectToAction("Module", "Admin", new { id = "phieu-dat-san" });
    }

    private async Task FillBookingOptionsAsync(StaffBookingCreateViewModel model)
    {
        model.KhachHangOptions = await _db.KhachHangs.AsNoTracking().Where(x => x.TrangThai)
            .OrderBy(x => x.HoTen)
            .Select(x => new SelectListItem(x.HoTen + (x.SoDienThoai == null ? "" : " · " + x.SoDienThoai), x.MaKhachHang.ToString()))
            .ToListAsync();
        model.SanOptions = await _db.SanBongs.AsNoTracking().Where(x => x.TrangThai == "Hoạt động")
            .OrderBy(x => x.TenSan)
            .Select(x => new SelectListItem(x.TenSan, x.MaSan.ToString()))
            .ToListAsync();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateBookingStatus(int id, string status)
    {
        var (account, employee) = await LoadStaffContextAsync();
        if (account == null || employee is { TrangThai: false })
        {
            await HttpContext.SignOutAsync("Cookies");
            return RedirectToAction("Login", "Account");
        }
        if (employee == null)
        {
            TempData["AdminError"] = "Tài khoản chưa được liên kết với hồ sơ nhân viên.";
            return RedirectToAction(nameof(Index));
        }

        if (status is not ("Đã xác nhận" or "Đã hủy"))
            return BadRequest();

        var booking = await _db.PhieuDatSans.FirstOrDefaultAsync(x => x.MaPhieu == id);
        if (booking == null) return NotFound();
        if (booking.TrangThai != "Chờ xác nhận")
        {
            TempData["AdminError"] = "Phiếu này không còn ở trạng thái chờ xác nhận.";
            return RedirectToAction("Module", "Admin", new { id = "phieu-dat-san" });
        }

        booking.TrangThai = status;
        await _db.SaveChangesAsync();
        TempData["AdminSuccess"] = status == "Đã xác nhận"
            ? $"Đã xác nhận phiếu đặt sân #{id}."
            : $"Đã hủy phiếu đặt sân #{id}.";
        return RedirectToAction("Module", "Admin", new { id = "phieu-dat-san" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateInvoice(int bookingId)
    {
        var (account, employee) = await LoadStaffContextAsync();
        if (account == null || employee is { TrangThai: false })
        {
            await HttpContext.SignOutAsync("Cookies");
            return RedirectToAction("Login", "Account");
        }
        if (employee == null)
        {
            TempData["AdminError"] = "Tài khoản chưa được liên kết với hồ sơ nhân viên.";
            return RedirectToAction(nameof(Index));
        }

        var booking = await _db.PhieuDatSans.FirstOrDefaultAsync(x => x.MaPhieu == bookingId);
        if (booking == null) return NotFound();
        if (booking.TrangThai != "Đã xác nhận")
        {
            TempData["AdminError"] = "Chỉ lập hóa đơn cho phiếu đã được xác nhận.";
            return RedirectToAction("Module", "Admin", new { id = "phieu-dat-san" });
        }
        if (await _db.HoaDons.AnyAsync(x => x.MaPhieu == bookingId))
        {
            TempData["AdminError"] = "Phiếu này đã có hóa đơn.";
            return RedirectToAction("Module", "Admin", new { id = "phieu-dat-san" });
        }

        _db.HoaDons.Add(new HoaDon
        {
            MaPhieu = booking.MaPhieu,
            MaNhanVienLap = employee.MaNhanVien,
            NgayLap = DateTime.Now,
            TongTienSan = booking.TongTienSan,
            TongTienDichVu = 0,
            TongCong = booking.TongTienSan,
            TrangThai = false
        });

        try
        {
            await _db.SaveChangesAsync();
            TempData["AdminSuccess"] = $"Đã lập hóa đơn cho phiếu #{bookingId}.";
        }
        catch (DbUpdateException)
        {
            _db.ChangeTracker.Clear();
            TempData["AdminError"] = "Không thể lập hóa đơn. Hãy tải lại danh sách để kiểm tra phiếu đã có hóa đơn chưa.";
        }

        return RedirectToAction("Module", "Admin", new { id = "phieu-dat-san" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkInvoicePaid(int id, string paymentMethod)
    {
        var (account, employee) = await LoadStaffContextAsync();
        if (account == null || employee is { TrangThai: false })
        {
            await HttpContext.SignOutAsync("Cookies");
            return RedirectToAction("Login", "Account");
        }
        if (employee == null)
        {
            TempData["AdminError"] = "Tài khoản chưa được liên kết với hồ sơ nhân viên.";
            return RedirectToAction(nameof(Index));
        }

        var allowedMethods = new[] { "Tiền mặt", "Chuyển khoản", "Thẻ", "Ví điện tử" };
        if (!allowedMethods.Contains(paymentMethod))
            return BadRequest();

        var invoice = await _db.HoaDons.FirstOrDefaultAsync(x => x.MaHoaDon == id);
        if (invoice == null) return NotFound();
        if (invoice.TrangThai)
        {
            TempData["AdminError"] = "Hóa đơn này đã được thanh toán.";
            return RedirectToAction("Module", "Admin", new { id = "hoa-don" });
        }

        invoice.PhuongThucThanhToan = paymentMethod;
        invoice.TrangThai = true;
        await _db.SaveChangesAsync();
        TempData["AdminSuccess"] = $"Đã ghi nhận thanh toán hóa đơn #{id} bằng {paymentMethod}.";
        return RedirectToAction("Module", "Admin", new { id = "hoa-don" });
    }

    private async Task<(NguoiDung? Account, NhanVien? Employee)> LoadStaffContextAsync()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var accountId))
            return (null, null);

        var account = await _db.NguoiDungs.FirstOrDefaultAsync(x => x.MaNguoiDung == accountId);
        if (account == null || !account.TrangThai)
            return (null, null);

        NhanVien? employee = null;
        if (!string.IsNullOrWhiteSpace(account.Email))
            employee = await _db.NhanViens.FirstOrDefaultAsync(x => x.Email == account.Email);
        if (employee == null && !string.IsNullOrWhiteSpace(account.SoDienThoai))
            employee = await _db.NhanViens.FirstOrDefaultAsync(x => x.SoDienThoai == account.SoDienThoai);
        if (employee == null)
            employee = await _db.NhanViens.FirstOrDefaultAsync(x => x.HoTen == account.HoTen);

        return (account, employee);
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
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        var currentTime = DateTime.Now.TimeOfDay;
        ViewBag.ScheduleCount = employeeId == null ? 0 : await _db.LichLams.CountAsync(x =>
            x.MaNhanVien == employeeId && x.TrangThai && (x.NgayLam >= tomorrow ||
            (x.NgayLam >= today && x.NgayLam < tomorrow && (x.CaLamViec == null || x.CaLamViec.ThoiGianKetThuc > currentTime))));
        ViewBag.ReviewCount = await _db.DanhGias.CountAsync();
        ViewBag.NotificationCount = await _db.ThongBaos.CountAsync(x => x.MaNguoiDung == accountId);
        ViewBag.SupportCount = await _db.HoTros.CountAsync();
    }
}
