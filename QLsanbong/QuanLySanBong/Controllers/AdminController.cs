using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLySanBong.Data;
using QuanLySanBong.Models;
using QuanLySanBong.ViewModels;

namespace QuanLySanBong.Controllers;

[Authorize(Roles = "Admin,Super Admin,Quản trị viên,Quản lý sân,Nhân viên,Nhân viên thu ngân")]
public partial class AdminController : Controller
{
    private readonly AppDbContext _db;
    public AdminController(AppDbContext db) => _db = db;

    private IActionResult RejectCustomerManagement()
    {
        TempData["AdminError"] = "Mục khách hàng chỉ cho phép xem thông tin chi tiết.";
        return RedirectToAction(nameof(Module), new { id = "khach-hang" });
    }

    [HttpGet]
[Authorize(Roles = "Admin,Super Admin,Quản trị viên,Quản lý sân")]
    public async Task<IActionResult> ViTriSan()
    {
        ViewBag.ActiveModule = "vi-tri-san";
        ViewBag.TinhThanhs = TinhThanhVietNam.DanhSach;
        ViewData["Title"] = "Địa chỉ sân";
        var courts = await _db.SanBongs.AsNoTracking().Include(s => s.LoaiSan).OrderBy(s => s.TenSan).ToListAsync();
        return View(courts);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
[Authorize(Roles = "Admin,Super Admin,Quản trị viên,Quản lý sân")]
    public async Task<IActionResult> CapNhatViTriSan(int maSan, string? tinhThanh, string? diaChiChiTiet)
    {
        if (string.IsNullOrWhiteSpace(tinhThanh) || !TinhThanhVietNam.DanhSach.Contains(tinhThanh))
        {
            TempData["AdminError"] = "Hãy chọn tỉnh/thành phố hợp lệ.";
            return RedirectToAction(nameof(ViTriSan));
        }

        var court = await _db.SanBongs.FirstOrDefaultAsync(s => s.MaSan == maSan);
        if (court == null) return NotFound();
        var address = string.IsNullOrWhiteSpace(diaChiChiTiet)
            ? tinhThanh
            : $"{diaChiChiTiet.Trim()}, {tinhThanh}";
        court.DiaChi = address[..Math.Min(address.Length, 255)];
        await _db.SaveChangesAsync();
        TempData["AdminSuccess"] = $"Đã cập nhật vị trí {court.TenSan}.";
        return RedirectToAction(nameof(ViTriSan));
    }

    [HttpGet]
[Authorize(Roles = "Admin,Super Admin,Quản trị viên,Quản lý sân")]
    public async Task<IActionResult> Index()
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        var sixMonthsAgo = new DateTime(today.Year, today.Month, 1).AddMonths(-5);
        var activeFields = await _db.SanBongs.CountAsync(s => s.TrangThai == "Hoạt động");
        var totalFields = await _db.SanBongs.CountAsync();
        var bookingsToday = await _db.PhieuDatSans.CountAsync(p => p.NgayDat >= today && p.NgayDat < tomorrow);
        var pendingBookings = await _db.PhieuDatSans.CountAsync(p => p.TrangThai == "Chờ xác nhận");
        var revenueToday = await _db.HoaDons.Where(h => h.NgayLap >= today && h.NgayLap < tomorrow && h.TrangThai)
            .SumAsync(h => (decimal?)h.TongCong) ?? 0m;
        var recentBookings = await _db.PhieuDatSans.AsNoTracking()
            .Include(p => p.KhachHang).Include(p => p.SanBong)
            .OrderByDescending(p => p.NgayTao).Take(8)
            .Select(p => new AdminBookingRow
            {
                Id = p.MaPhieu,
                Customer = p.KhachHang != null ? p.KhachHang.HoTen : "Khách vãng lai",
                Field = p.SanBong != null ? p.SanBong.TenSan : "—",
                Date = p.NgayDat,
                Start = p.GioBatDau,
                End = p.GioKetThuc,
                Amount = p.TongTienSan,
                Status = p.TrangThai
            }).ToListAsync();
        var invoiceHistory = await _db.HoaDons.AsNoTracking()
            .Where(h => h.NgayLap >= sixMonthsAgo && h.TrangThai)
            .Select(h => new { h.NgayLap, h.TongCong }).ToListAsync();
        var monthlyRevenue = Enumerable.Range(0, 6).Select(offset =>
        {
            var month = sixMonthsAgo.AddMonths(offset);
            return new AdminRevenuePoint
            {
                Month = month.ToString("MMM"),
                Amount = invoiceHistory.Where(h => h.NgayLap.Year == month.Year && h.NgayLap.Month == month.Month).Sum(h => h.TongCong)
            };
        }).ToList();

        var model = new AdminDashboardViewModel
        {
            RevenueToday = revenueToday,
            BookingsToday = bookingsToday,
            ActiveFields = activeFields,
            TotalFields = totalFields,
            PendingBookings = pendingBookings,
            FillRate = activeFields == 0 ? 0 : Math.Min(100, (int)Math.Round((double)bookingsToday * 100 / activeFields)),
            CustomerCount = await _db.KhachHangs.CountAsync(),
            EmployeeCount = await _db.NhanViens.CountAsync(),
            ServiceCount = await _db.DichVus.CountAsync(),
            InvoiceCount = await _db.HoaDons.CountAsync(),
            FieldTypeCount = await _db.LoaiSans.CountAsync(),
            PriceCount = await _db.BangGias.CountAsync(),
            PaymentTypeCount = await _db.LoaiThanhToans.CountAsync(),
            ShiftCount = await _db.CaLamViecs.CountAsync(),
            ScheduleCount = await _db.LichLams.CountAsync(),
            ReviewCount = await _db.DanhGias.CountAsync(),
            NotificationCount = await _db.ThongBaos.CountAsync(),
            SupportCount = await _db.HoTros.CountAsync(),
            UserCount = await _db.NguoiDungs.CountAsync(),
            RoleCount = await _db.VaiTros.CountAsync(),
            LogCount = await _db.LichSus.CountAsync(),
            RecentBookings = recentBookings,
            MonthlyRevenue = monthlyRevenue
        };
        SetSidebarCounts(model);
        ViewBag.ActiveModule = "dashboard";
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Module(string id)
    {
        var isStaff = User.IsInRole("Nhân viên") || User.IsInRole("Nhân viên thu ngân");
        var staffModules = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "san-bong", "bang-gia", "khach-hang", "phieu-dat-san", "dich-vu", "hoa-don",
            "chi-tiet-hoa-don", "lich-lam", "danh-gia", "thong-bao", "ho-tro"
        };
        if (isStaff && !staffModules.Contains(id ?? string.Empty)) return Forbid();

        var accountId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : 0;
        var account = isStaff ? await _db.NguoiDungs.AsNoTracking().FirstOrDefaultAsync(x => x.MaNguoiDung == accountId) : null;
        NhanVien? employee = null;
        if (account != null && !string.IsNullOrWhiteSpace(account.Email))
            employee = await _db.NhanViens.AsNoTracking().FirstOrDefaultAsync(x => x.Email == account.Email);
        if (account != null && employee == null && !string.IsNullOrWhiteSpace(account.SoDienThoai))
            employee = await _db.NhanViens.AsNoTracking().FirstOrDefaultAsync(x => x.SoDienThoai == account.SoDienThoai);
        if (account != null && employee == null)
            employee = await _db.NhanViens.AsNoTracking().FirstOrDefaultAsync(x => x.HoTen == account.HoTen);

        if (isStaff && (account == null || !account.TrangThai || employee is { TrangThai: false }))
        {
            await HttpContext.SignOutAsync("Cookies");
            return RedirectToAction("Login", "Account");
        }
        IQueryable<LichLam> scheduleQuery = _db.LichLams.AsNoTracking().Include(x => x.NhanVien).Include(x => x.CaLamViec);
        IQueryable<ThongBao> notificationQuery = _db.ThongBaos.AsNoTracking().Include(x => x.NguoiDung);
        if (isStaff)
        {
            var employeeId = employee?.MaNhanVien ?? 0;
            scheduleQuery = scheduleQuery.Where(x => x.MaNhanVien == employeeId);
            notificationQuery = notificationQuery.Where(x => x.MaNguoiDung == accountId);
        }

        AdminModuleViewModel? model = id?.ToLowerInvariant() switch
        {
            "loai-san" => Create(await _db.LoaiSans.AsNoTracking().ToListAsync(), "loai-san", "Loại sân", "Danh mục loại sân và số lượng cầu thủ tối đa.", x => x.MaLoaiSan.ToString(), ("Tên loại sân", x => x.TenLoaiSan), ("Số người tối đa", x => x.SoLuongToiDa.ToString())),
            "san-bong" => Create(await _db.SanBongs.AsNoTracking().Include(x => x.LoaiSan).ToListAsync(), "san-bong", "Sân bóng", "Danh sách sân và tình trạng hoạt động.", x => x.MaSan.ToString(), ("Tên sân", x => x.TenSan), ("Loại sân", x => x.LoaiSan?.TenLoaiSan ?? "—"), ("Trạng thái", x => x.TrangThai), ("Mô tả", x => x.MoTa ?? "—")),
            "bang-gia" => Create(await _db.BangGias.AsNoTracking().Include(x => x.SanBong).ToListAsync(), "bang-gia", "Bảng giá", "Khung giờ và giá thuê được thiết lập cho từng sân.", x => x.MaGia.ToString(), ("Sân", x => x.SanBong?.TenSan ?? "—"), ("Khung giờ", x => $"{x.GioBatDau:hh\\:mm} – {x.GioKetThuc:hh\\:mm}"), ("Giá", x => x.GiaTien.ToString("N0") + " đ"), ("Loại ngày", x => x.LoaiNgay ?? "Tất cả ngày")),
            "khach-hang" => Create(await _db.KhachHangs.AsNoTracking().ToListAsync(), "khach-hang", "Khách hàng", "Hồ sơ và thông tin liên hệ khách hàng.", x => x.MaKhachHang.ToString(), ("Họ tên", x => x.HoTen), ("Điện thoại", x => x.SoDienThoai ?? "—"), ("Email", x => x.Email ?? "—"), ("Địa chỉ", x => x.DiaChi ?? "—"), ("Lý do khóa", x => x.LyDoKhoa ?? "—"), ("Trạng thái", x => x.TrangThai ? "Hoạt động" : "Ngừng hoạt động")),
            "phieu-dat-san" => Create(await _db.PhieuDatSans.AsNoTracking().Include(x => x.KhachHang).Include(x => x.SanBong).Include(x => x.HoaDon).OrderByDescending(x => x.NgayTao).ToListAsync(), "phieu-dat-san", "Phiếu đặt sân", "Theo dõi lịch đặt, khách hàng và trạng thái xác nhận.", x => x.MaPhieu.ToString(), ("Khách hàng", x => x.KhachHang?.HoTen ?? "—"), ("Sân", x => x.SanBong?.TenSan ?? "—"), ("Ngày", x => x.NgayDat.ToString("dd/MM/yyyy")), ("Giờ", x => $"{x.GioBatDau:hh\\:mm} – {x.GioKetThuc:hh\\:mm}"), ("Tổng tiền sân", x => x.TongTienSan.ToString("N0") + " đ"), ("Hóa đơn", x => x.HoaDon == null ? "Chưa lập" : x.HoaDon.TrangThai ? "Đã thanh toán" : "Chưa thanh toán"), ("Trạng thái", x => x.TrangThai)),
            "dich-vu" => Create(await _db.DichVus.AsNoTracking().ToListAsync(), "dich-vu", "Dịch vụ", "Nước uống, thuê dụng cụ và các dịch vụ đi kèm.", x => x.MaDichVu.ToString(), ("Tên dịch vụ", x => x.TenDichVu), ("Đơn giá", x => x.DonGia.ToString("N0") + " đ"), ("Đơn vị", x => x.DonViTinh ?? "—"), ("Tồn kho", x => x.SoLuongTon.ToString())),
            "hoa-don" => Create(await _db.HoaDons.AsNoTracking().Include(x => x.PhieuDatSan).Include(x => x.NhanVienLap).OrderByDescending(x => x.NgayLap).ToListAsync(), "hoa-don", "Hóa đơn", "Tổng hợp các hóa đơn và tình trạng thanh toán.", x => x.MaHoaDon.ToString(), ("Ngày lập", x => x.NgayLap.ToString("dd/MM/yyyy HH:mm")), ("Mã phiếu", x => x.MaPhieu.ToString()), ("Nhân viên lập", x => x.NhanVienLap?.HoTen ?? "—"), ("Tổng cộng", x => x.TongCong.ToString("N0") + " đ"), ("Thanh toán", x => x.PhuongThucThanhToan ?? "—"), ("Trạng thái", x => x.TrangThai ? "Đã thanh toán" : "Chưa thanh toán")),
            "chi-tiet-hoa-don" => Create(await _db.ChiTietHoaDons.AsNoTracking().Include(x => x.HoaDon).Include(x => x.DichVu).ToListAsync(), "chi-tiet-hoa-don", "Chi tiết hóa đơn", "Các khoản dịch vụ và chi phí trong từng hóa đơn.", x => x.MaChiTiet.ToString(), ("Hóa đơn", x => x.MaHoaDon.ToString()), ("Dịch vụ", x => x.DichVu?.TenDichVu ?? "—"), ("Số lượng", x => x.SoLuong.ToString()), ("Đơn giá", x => x.DonGia.ToString("N0") + " đ"), ("Thành tiền", x => x.ThanhTien.ToString("N0") + " đ")),
            "loai-thanh-toan" => Create(await _db.LoaiThanhToans.AsNoTracking().ToListAsync(), "loai-thanh-toan", "Loại thanh toán", "Các phương thức thanh toán được hỗ trợ.", x => x.MaLoaiTT.ToString(), ("Tên phương thức", x => x.TenLoaiTT)),
            "nhan-vien" => Create(await _db.NhanViens.AsNoTracking().ToListAsync(), "nhan-vien", "Nhân viên", "Thông tin nhân sự và trạng thái làm việc.", x => x.MaNhanVien.ToString(), ("Họ tên", x => x.HoTen), ("Chức vụ", x => x.ChucVu ?? "—"), ("Điện thoại", x => x.SoDienThoai ?? "—"), ("Email", x => x.Email ?? "—"), ("Lý do khóa", x => x.LyDoKhoa ?? "—"), ("Trạng thái", x => x.TrangThai ? "Đang làm" : "Ngừng làm")),
            "ca-lam-viec" => Create(await _db.CaLamViecs.AsNoTracking().Include(x => x.NhanVien).ToListAsync(), "ca-lam-viec", "Ca làm việc", "Các ca trực và nhân viên phụ trách.", x => x.MaCa.ToString(), ("Tên ca", x => x.TenCa), ("Nhân viên", x => x.NhanVien?.HoTen ?? "—"), ("Khung giờ", x => $"{x.ThoiGianBatDau:hh\\:mm} – {x.ThoiGianKetThuc:hh\\:mm}"), ("Thứ", x => x.ThuTrongTuan ?? "Mỗi ngày")),
            "lich-lam" => Create(await scheduleQuery.OrderByDescending(x => x.NgayLam).ToListAsync(), "lich-lam", "Lịch làm", "Lịch phân công nhân viên theo ngày và ca.", x => x.MaLich.ToString(), ("Nhân viên", x => x.NhanVien?.HoTen ?? "—"), ("Ca", x => x.CaLamViec?.TenCa ?? "—"), ("Ngày làm", x => x.NgayLam.ToString("dd/MM/yyyy")), ("Trạng thái", x => x.TrangThai ? "Đã xếp lịch" : "Đã hủy")),
            "danh-gia" => Create(await _db.DanhGias.AsNoTracking().Include(x => x.NguoiDung).Include(x => x.PhieuDatSan).OrderByDescending(x => x.NgayDanhGia).ToListAsync(), "danh-gia", "Đánh giá", "Phản hồi và đánh giá của khách hàng.", x => x.MaDanhGia.ToString(), ("Khách hàng", x => x.NguoiDung?.HoTen ?? "—"), ("Số sao", x => x.SoSao + " / 5"), ("Nội dung", x => x.NoiDung ?? "—"), ("Ngày gửi", x => x.NgayDanhGia.ToString("dd/MM/yyyy")), ("Mã phiếu", x => x.MaPhieu.ToString())),
            "thong-bao" => Create(await notificationQuery.OrderByDescending(x => x.NgayGui).ToListAsync(), "thong-bao", "Thông báo", "Thông báo hệ thống và ưu đãi đã gửi cho người dùng.", x => x.MaThongBao.ToString(), ("Người nhận", x => x.NguoiDung?.HoTen ?? "—"), ("Nội dung", x => x.NoiDung), ("Ngày gửi", x => x.NgayGui.ToString("dd/MM/yyyy HH:mm")), ("Trạng thái", x => x.TrangThai ? "Đã đọc" : "Chưa đọc")),
            "ho-tro" => Create(await _db.HoTros.AsNoTracking().Include(x => x.NguoiDung).OrderByDescending(x => x.NgayTao).ToListAsync(), "ho-tro", "Yêu cầu hỗ trợ", "Tiếp nhận và theo dõi các yêu cầu của khách hàng.", x => x.MaHoTro.ToString(), ("Tiêu đề", x => x.TieuDe), ("Khách hàng", x => x.NguoiDung?.HoTen ?? "—"), ("Nội dung", x => x.NoiDung ?? "—"), ("Ngày tạo", x => x.NgayTao.ToString("dd/MM/yyyy HH:mm")), ("Trạng thái", x => x.TrangThai)),
            "nguoi-dung" => Create(await _db.NguoiDungs.AsNoTracking().Include(x => x.VaiTro).ToListAsync(), "nguoi-dung", "Người dùng", "Tài khoản và vai trò truy cập hệ thống.", x => x.MaNguoiDung.ToString(), ("Tên đăng nhập", x => x.TenDangNhap), ("Họ tên", x => x.HoTen), ("Vai trò", x => x.VaiTro?.TenVaiTro ?? "—"), ("Email", x => x.Email ?? "—"), ("Lý do khóa", x => x.LyDoKhoa ?? "—"), ("Trạng thái", x => x.TrangThai ? "Hoạt động" : "Đã khóa")),
            "vai-tro" => Create(await _db.VaiTros.AsNoTracking().Include(x => x.NguoiDungs).ToListAsync(), "vai-tro", "Vai trò", "Nhóm quyền và số người dùng đang được gán.", x => x.MaVaiTro.ToString(), ("Tên vai trò", x => x.TenVaiTro), ("Số người dùng", x => x.NguoiDungs.Count.ToString())),
            "lich-su" => Create(await _db.LichSus.AsNoTracking().Include(x => x.NguoiDung).Include(x => x.LoaiThanhToan).OrderByDescending(x => x.ThoiGian).ToListAsync(), "lich-su", "Lịch sử hoạt động", "Nhật ký thao tác và giao dịch trên hệ thống.", x => x.MaLichSu.ToString(), ("Người dùng", x => x.NguoiDung?.HoTen ?? "—"), ("Hành động", x => x.HanhDong), ("Mô tả", x => x.MoTa ?? "—"), ("Thời gian", x => x.ThoiGian.ToString("dd/MM/yyyy HH:mm")), ("Loại thanh toán", x => x.LoaiThanhToan?.TenLoaiTT ?? "—")),
            _ => null
        };

        if (model == null) return NotFound();
        await SetSidebarCounts();
        if (isStaff)
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var currentTime = DateTime.Now.TimeOfDay;
            ViewBag.ScheduleCount = employee == null ? 0 : await _db.LichLams.CountAsync(x =>
                x.MaNhanVien == employee.MaNhanVien && x.TrangThai && (x.NgayLam >= tomorrow ||
                (x.NgayLam >= today && x.NgayLam < tomorrow && (x.CaLamViec == null || x.CaLamViec.ThoiGianKetThuc > currentTime))));
            ViewBag.NotificationCount = await _db.ThongBaos.CountAsync(x => x.MaNguoiDung == accountId);
        }
        ViewBag.StaffReadOnly = isStaff;
        ViewBag.StaffOperationsEnabled = !isStaff || employee is { TrangThai: true };
        ViewBag.ActiveModule = model.Id;
        return View(model);
    }

    private async Task SetSidebarCounts()
    {
        ViewBag.FieldTypeCount = await _db.LoaiSans.CountAsync();
        ViewBag.FieldCount = await _db.SanBongs.CountAsync();
        ViewBag.PriceCount = await _db.BangGias.CountAsync();
        ViewBag.CustomerCount = await _db.KhachHangs.CountAsync();
        ViewBag.PendingCount = await _db.PhieuDatSans.CountAsync(x => x.TrangThai == "Chờ xác nhận");
        ViewBag.ServiceCount = await _db.DichVus.CountAsync();
        ViewBag.InvoiceCount = await _db.HoaDons.CountAsync();
        ViewBag.InvoiceDetailCount = await _db.ChiTietHoaDons.CountAsync();
        ViewBag.PaymentTypeCount = await _db.LoaiThanhToans.CountAsync();
        ViewBag.EmployeeCount = await _db.NhanViens.CountAsync();
        ViewBag.ScheduleCount = await _db.LichLams.CountAsync();
        ViewBag.ReviewCount = await _db.DanhGias.CountAsync();
        ViewBag.NotificationCount = await _db.ThongBaos.CountAsync();
        ViewBag.SupportCount = await _db.HoTros.CountAsync();
        ViewBag.UserCount = await _db.NguoiDungs.CountAsync();
        ViewBag.LogCount = await _db.LichSus.CountAsync();
    }

    private void SetSidebarCounts(AdminDashboardViewModel model)
    {
        ViewBag.FieldTypeCount = model.FieldTypeCount;
        ViewBag.FieldCount = model.TotalFields;
        ViewBag.PriceCount = model.PriceCount;
        ViewBag.CustomerCount = model.CustomerCount;
        ViewBag.PendingCount = model.PendingBookings;
        ViewBag.ServiceCount = model.ServiceCount;
        ViewBag.InvoiceCount = model.InvoiceCount;
        ViewBag.PaymentTypeCount = model.PaymentTypeCount;
        ViewBag.EmployeeCount = model.EmployeeCount;
        ViewBag.ScheduleCount = model.ScheduleCount;
        ViewBag.ReviewCount = model.ReviewCount;
        ViewBag.NotificationCount = model.NotificationCount;
        ViewBag.SupportCount = model.SupportCount;
        ViewBag.UserCount = model.UserCount;
        ViewBag.LogCount = model.LogCount;
    }

    private static AdminModuleViewModel Create<T>(IReadOnlyCollection<T> data, string id, string title, string description, Func<T, string> key, params (string Header, Func<T, string> Value)[] columns)
    {
        return new AdminModuleViewModel
        {
            Id = id,
            Title = title,
            Description = description,
            Headers = columns.Select(column => column.Header).ToList(),
            Rows = data.Select(item => new AdminModuleRow
            {
                Key = key(item),
                Values = columns.Select(column => column.Value(item)).ToList()
            }).ToList()
        };
    }
}
