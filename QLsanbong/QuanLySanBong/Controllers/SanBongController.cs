using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLySanBong.Data;
using QuanLySanBong.Models;
using QuanLySanBong.ViewModels;

namespace QuanLySanBong.Controllers;

public class SanBongController : Controller
{
    private static readonly TimeSpan ThoiLuongMotTran = TimeSpan.FromMinutes(90);
    private static readonly List<string> TinhThanhOptions = TinhThanhVietNam.DanhSach;
    private readonly AppDbContext _db;

    public SanBongController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Index(int? soNguoi, string? diaDiem, double? viDo, double? kinhDo)
    {
        var query = _db.SanBongs.AsNoTracking().Include(s => s.LoaiSan).Include(s => s.BangGias)
            .Where(s => s.TrangThai == "Hoạt động");
        if (soNguoi.HasValue)
            query = query.Where(s => s.LoaiSan != null && s.LoaiSan.SoLuongToiDa == soNguoi.Value);
        if (!string.IsNullOrWhiteSpace(diaDiem))
        {
            var selectedLocation = diaDiem.Trim();
            var locationPattern = $"%{selectedLocation}%";
            query = query.Where(s => s.DiaChi != null && EF.Functions.Like(s.DiaChi, locationPattern));
        }

        var fields = await query.OrderBy(s => s.TenSan).ToListAsync();
        var hasUserLocation = viDo is >= -90 and <= 90 && kinhDo is >= -180 and <= 180;
        var model = new SanBongListViewModel
        {
            SoNguoi = soNguoi,
            DiaDiem = diaDiem,
            DiaDiemOptions = TinhThanhOptions,
            ViDoNguoiDung = hasUserLocation ? viDo : null,
            KinhDoNguoiDung = hasUserLocation ? kinhDo : null,
            SanBongs = fields.Select(s => new SanBongCardViewModel
            {
                MaSan = s.MaSan,
                TenSan = s.TenSan,
                TenLoaiSan = s.LoaiSan?.TenLoaiSan ?? "Sân bóng",
                SoNguoi = s.LoaiSan?.SoLuongToiDa ?? 0,
                MoTa = s.MoTa,
                DiaChi = s.DiaChi,
                HinhAnh = !string.IsNullOrWhiteSpace(s.HinhAnh) ? s.HinhAnh : AnhMacDinh(s.LoaiSan?.SoLuongToiDa ?? 0),
                GiaTu = s.BangGias.Select(g => (decimal?)g.GiaTien).Min(),
                KhoangCachKm = hasUserLocation && s.ViDo.HasValue && s.KinhDo.HasValue
                    ? TinhKhoangCachKm(viDo!.Value, kinhDo!.Value, s.ViDo.Value, s.KinhDo.Value)
                    : null
            }).OrderBy(card => card.KhoangCachKm ?? double.MaxValue).ToList()
        };
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ChiTiet(int id, DateTime? ngay)
    {
        var field = await _db.SanBongs.AsNoTracking().Include(s => s.LoaiSan)
            .FirstOrDefaultAsync(s => s.MaSan == id && s.TrangThai == "Hoạt động");
        if (field == null) return NotFound();

        var selectedDate = ngay?.Date ?? DateTime.Today;
        if (selectedDate < DateTime.Today) selectedDate = DateTime.Today;
        var prices = await _db.BangGias.AsNoTracking().Where(g => g.MaSan == id).ToListAsync();
        var dayType = selectedDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ? "Cuối tuần" : "Ngày thường";
        var dayPrices = prices.Where(p => string.IsNullOrWhiteSpace(p.LoaiNgay) || p.LoaiNgay == "Tất cả" || p.LoaiNgay == dayType).ToList();
        var reservations = await _db.PhieuDatSans.AsNoTracking()
            .Where(p => p.MaSan == id && p.NgayDat == selectedDate)
            .Select(p => new { p.GioBatDau, p.GioKetThuc, p.TrangThai })
            .ToListAsync();

        var slots = new List<SanBongSlotViewModel>();
        foreach (var price in dayPrices.OrderBy(p => p.GioBatDau))
        {
            for (var start = price.GioBatDau; start + ThoiLuongMotTran <= price.GioKetThuc; start += ThoiLuongMotTran)
            {
                var end = start + ThoiLuongMotTran;
                var isBooked = reservations.Any(r => !LaPhieuDaHuy(r.TrangThai) && start < r.GioKetThuc && end > r.GioBatDau);
                slots.Add(new SanBongSlotViewModel
                {
                    BatDau = start,
                    KetThuc = end,
                    GiaMoiGio = price.GiaTien,
                    TongTien = decimal.Round(price.GiaTien * (decimal)ThoiLuongMotTran.TotalHours, 0),
                    ConTrong = !isBooked
                });
            }
        }

        var reviewQuery = _db.DanhGias.AsNoTracking().Where(r => r.MaSan == id);
        var reviewStats = await reviewQuery.Select(r => (double?)r.SoSao).AverageAsync();
        var reviews = await reviewQuery.Include(r => r.NguoiDung).OrderByDescending(r => r.NgayDanhGia).Take(12)
            .Select(r => new SanBongReviewViewModel
            {
                TenNguoiDung = r.NguoiDung != null ? r.NguoiDung.HoTen : "Người chơi",
                SoSao = r.SoSao,
                NoiDung = r.NoiDung ?? "",
                NgayDanhGia = r.NgayDanhGia
            }).ToListAsync();

        var model = new SanBongDetailViewModel
        {
            MaSan = field.MaSan,
            TenSan = field.TenSan,
            TenLoaiSan = field.LoaiSan?.TenLoaiSan ?? "Sân bóng",
            SoNguoi = field.LoaiSan?.SoLuongToiDa ?? 0,
            MoTa = field.MoTa,
            DiaChi = field.DiaChi,
            ViDo = field.ViDo,
            KinhDo = field.KinhDo,
            HinhAnh = !string.IsNullOrWhiteSpace(field.HinhAnh) ? field.HinhAnh : AnhMacDinh(field.LoaiSan?.SoLuongToiDa ?? 0),
            NgayChoi = selectedDate,
            GiaTu = dayPrices.Select(p => (decimal?)p.GiaTien).Min(),
            DiemTrungBinh = reviewStats ?? 0,
            SoDanhGia = await reviewQuery.CountAsync(),
            KhungGios = slots,
            DanhGias = reviews
        };

        return View(model);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> ThanhToan(int maSan, DateTime ngayDat, TimeSpan gioBatDau, TimeSpan gioKetThuc)
    {
        var model = await TaoThongTinThanhToanAsync(maSan, ngayDat, gioBatDau, gioKetThuc);
        if (model == null) return RedirectToAction(nameof(ChiTiet), new { id = maSan, ngay = ngayDat.ToString("yyyy-MM-dd") });
        return View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ThanhToan(DatSanThanhToanViewModel input)
    {
        var checkout = await TaoThongTinThanhToanAsync(input.MaSan, input.NgayDat, input.GioBatDau, input.GioKetThuc);
        if (checkout == null)
            return RedirectToAction(nameof(ChiTiet), new { id = input.MaSan, ngay = input.NgayDat.ToString("yyyy-MM-dd") });

        checkout.PhuongThuc = input.PhuongThuc;
        if (!checkout.PhuongThucs.Contains(input.PhuongThuc))
        {
            checkout.LoiLich = "Vui lòng chọn phương thức thanh toán hợp lệ.";
            return View(checkout);
        }

        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Challenge();
        var user = await _db.NguoiDungs.FirstOrDefaultAsync(u => u.MaNguoiDung == userId && u.TrangThai);
        if (user == null) return Challenge();

        var customer = await TimKhachHangAsync(user);
        if (customer == null)
        {
            customer = new KhachHang
            {
                HoTen = user.HoTen,
                Email = user.Email,
                SoDienThoai = user.SoDienThoai,
                NgayTao = DateTime.Now,
                TrangThai = true
            };
            _db.KhachHangs.Add(customer);
            await _db.SaveChangesAsync();
        }

        var booking = new PhieuDatSan
        {
            MaKhachHang = customer.MaKhachHang,
            MaSan = checkout.MaSan,
            NgayDat = checkout.NgayDat.Date,
            GioBatDau = checkout.GioBatDau,
            GioKetThuc = checkout.GioKetThuc,
            TongTienSan = checkout.TongTien,
            TrangThai = "Chờ xác nhận",
            GhiChu = $"Phương thức thanh toán: {checkout.PhuongThuc}. Chờ nhân viên xác nhận.",
            NgayTao = DateTime.Now
        };
        _db.PhieuDatSans.Add(booking);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(DatThanhCong), new { id = booking.MaPhieu });
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> DatThanhCong(int id)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Challenge();
        var user = await _db.NguoiDungs.AsNoTracking().FirstOrDefaultAsync(u => u.MaNguoiDung == userId);
        if (user == null) return Challenge();
        var customer = await TimKhachHangAsync(user);
        if (customer == null) return NotFound();
        var booking = await _db.PhieuDatSans.AsNoTracking().Include(p => p.SanBong)
            .FirstOrDefaultAsync(p => p.MaPhieu == id && p.MaKhachHang == customer.MaKhachHang);
        if (booking == null) return NotFound();
        var paymentMethod = booking.GhiChu?.Replace("Phương thức thanh toán: ", "").Replace(". Chờ nhân viên xác nhận.", "") ?? "Chưa chọn";
        return View(new DatSanHoanTatViewModel
        {
            MaPhieu = booking.MaPhieu,
            TenSan = booking.SanBong?.TenSan ?? "Sân bóng",
            NgayDat = booking.NgayDat,
            GioBatDau = booking.GioBatDau,
            GioKetThuc = booking.GioKetThuc,
            TongTien = booking.TongTienSan,
            TrangThai = booking.TrangThai,
            PhuongThuc = paymentMethod
        });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GuiDanhGia(int maSan, int soSao, string? noiDung)
    {
        if (soSao is < 1 or > 5 || !int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return RedirectToAction(nameof(ChiTiet), new { id = maSan });
        var user = await _db.NguoiDungs.FirstOrDefaultAsync(u => u.MaNguoiDung == userId && u.TrangThai);
        if (user == null) return Challenge();
        var review = await _db.DanhGias.FirstOrDefaultAsync(r => r.MaSan == maSan && r.MaNguoiDung == userId);
        if (review == null)
        {
            review = new DanhGia { MaSan = maSan, MaNguoiDung = user.MaNguoiDung };
            _db.DanhGias.Add(review);
        }
        review.SoSao = soSao;
        review.NoiDung = string.IsNullOrWhiteSpace(noiDung) ? null : noiDung.Trim()[..Math.Min(noiDung.Trim().Length, 500)];
        review.NgayDanhGia = DateTime.Now;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(ChiTiet), new { id = maSan, ngay = DateTime.Today.ToString("yyyy-MM-dd") });
    }

    private async Task<DatSanThanhToanViewModel?> TaoThongTinThanhToanAsync(int maSan, DateTime ngayDat, TimeSpan start, TimeSpan end)
    {
        var field = await _db.SanBongs.AsNoTracking().Include(s => s.LoaiSan)
            .FirstOrDefaultAsync(s => s.MaSan == maSan && s.TrangThai == "Hoạt động");
        if (field == null || ngayDat.Date < DateTime.Today || end <= start || end - start != ThoiLuongMotTran) return null;
        var dayType = ngayDat.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ? "Cuối tuần" : "Ngày thường";
        var rates = await _db.BangGias.AsNoTracking().Where(p => p.MaSan == maSan).ToListAsync();
        var rate = rates.Where(p => p.GioBatDau <= start && p.GioKetThuc >= end &&
                (string.IsNullOrWhiteSpace(p.LoaiNgay) || p.LoaiNgay == "Tất cả" || p.LoaiNgay == dayType))
            .OrderByDescending(p => p.LoaiNgay == dayType).FirstOrDefault();
        if (rate == null) return null;
        var conflict = await _db.PhieuDatSans.AnyAsync(p => p.MaSan == maSan && p.NgayDat == ngayDat.Date &&
            !p.TrangThai.Contains("hủy") && p.GioBatDau < end && p.GioKetThuc > start);
        if (conflict) return null;
        var methods = await _db.LoaiThanhToans.AsNoTracking().OrderBy(p => p.MaLoaiTT).Select(p => p.TenLoaiTT).ToListAsync();
        if (methods.Count == 0) methods = new List<string> { "Tiền mặt", "Chuyển khoản", "Thẻ" };
        var durationHours = (decimal)(end - start).TotalHours;
        return new DatSanThanhToanViewModel
        {
            MaSan = field.MaSan,
            TenSan = field.TenSan,
            TenLoaiSan = field.LoaiSan?.TenLoaiSan ?? "Sân bóng",
            HinhAnh = !string.IsNullOrWhiteSpace(field.HinhAnh) ? field.HinhAnh : AnhMacDinh(field.LoaiSan?.SoLuongToiDa ?? 0),
            NgayDat = ngayDat.Date,
            GioBatDau = start,
            GioKetThuc = end,
            GiaMoiGio = rate.GiaTien,
            TongTien = decimal.Round(rate.GiaTien * durationHours, 0),
            PhuongThucs = methods,
            PhuongThuc = methods[0]
        };
    }

    private async Task<KhachHang?> TimKhachHangAsync(NguoiDung user)
    {
        if (user.Email == null && user.SoDienThoai == null) return null;
        return await _db.KhachHangs.FirstOrDefaultAsync(k =>
            (user.Email != null && k.Email == user.Email) ||
            (user.SoDienThoai != null && k.SoDienThoai == user.SoDienThoai));
    }

    private static bool LaPhieuDaHuy(string status) => status.Contains("hủy", StringComparison.OrdinalIgnoreCase) || status.Contains("cancel", StringComparison.OrdinalIgnoreCase);

    private static string AnhMacDinh(int soNguoi) => soNguoi switch
    {
        5 => "https://images.unsplash.com/photo-1522778119026-d647f0596c20?auto=format&fit=crop&w=1200&q=80",
        7 => "https://images.unsplash.com/photo-1518604666860-9ed391f76460?auto=format&fit=crop&w=1200&q=80",
        11 => "https://images.unsplash.com/photo-1729843352938-0e10fbf96585?auto=format&fit=crop&w=1200&q=80",
        _ => "https://images.unsplash.com/photo-1522778119026-d647f0596c20?auto=format&fit=crop&w=1200&q=80"
    };

    private static double TinhKhoangCachKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double radiusKm = 6371;
        var dLat = Math.PI / 180 * (lat2 - lat1);
        var dLon = Math.PI / 180 * (lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(Math.PI / 180 * lat1) * Math.Cos(Math.PI / 180 * lat2) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return radiusKm * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }
}
