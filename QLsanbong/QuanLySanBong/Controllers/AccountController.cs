using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuanLySanBong.Data;
using QuanLySanBong.Models;
using QuanLySanBong.ViewModels;
using Microsoft.AspNetCore.Authorization;

namespace QuanLySanBong.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PasswordHasher<NguoiDung> _passwordHasher = new();

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var thongTinDangNhap = model.TenDangNhap.Trim().ToLower();

            var nguoiDung = await _context.NguoiDungs
                .Include(u => u.VaiTro)
                .FirstOrDefaultAsync(u =>
                    u.TenDangNhap.ToLower() == thongTinDangNhap ||
                    (u.Email != null && u.Email.ToLower() == thongTinDangNhap));

            if (nguoiDung == null || !nguoiDung.TrangThai)
            {
                ModelState.AddModelError(string.Empty, "Tên đăng nhập, email hoặc mật khẩu không đúng.");
                return View(model);
            }

            var ketQua = _passwordHasher.VerifyHashedPassword(
                nguoiDung,
                nguoiDung.MatKhau,
                model.MatKhau);

            if (ketQua == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(string.Empty, "Tên đăng nhập, email hoặc mật khẩu không đúng.");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, nguoiDung.MaNguoiDung.ToString()),
                new Claim(ClaimTypes.Name, nguoiDung.HoTen),
                new Claim(ClaimTypes.Role, nguoiDung.VaiTro?.TenVaiTro ?? "Khách hàng")
            };

            var identity = new ClaimsIdentity(claims, "Cookies");
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                "Cookies",
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = model.GhiNhoDangNhap
                });

            return RedirectToAction(nameof(Profile));
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var idDangNhap = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(idDangNhap, out var maNguoiDung))
                return RedirectToAction(nameof(Login));

            var nguoiDung = await _context.NguoiDungs
                .AsNoTracking()
                .Include(u => u.VaiTro)
                .FirstOrDefaultAsync(u => u.MaNguoiDung == maNguoiDung);

            if (nguoiDung == null || !nguoiDung.TrangThai)
            {
                await HttpContext.SignOutAsync("Cookies");
                return RedirectToAction(nameof(Login));
            }

            var model = new ProfileViewModel
            {
                HoTen = nguoiDung.HoTen,
                TenDangNhap = nguoiDung.TenDangNhap,
                Email = nguoiDung.Email ?? "Chưa cập nhật",
                SoDienThoai = nguoiDung.SoDienThoai ?? "Chưa cập nhật",
                TenVaiTro = nguoiDung.VaiTro?.TenVaiTro ?? "Khách hàng"
            };

            return View(model);
        }
        
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var tenDangNhap = model.TenDangNhap.Trim();
            var email = model.Email.Trim().ToLower();
            var soDienThoai = model.SoDienThoai.Trim();

            var biTrungTenDangNhap = await _context.NguoiDungs
                .AnyAsync(u => u.TenDangNhap.ToLower() == tenDangNhap.ToLower());

            if (biTrungTenDangNhap)
            {
                ModelState.AddModelError(nameof(model.TenDangNhap), "Tên đăng nhập này đã được sử dụng.");
                return View(model);
            }

            var biTrungEmail = await _context.NguoiDungs
                .AnyAsync(u => u.Email != null && u.Email.ToLower() == email);

            if (biTrungEmail)
            {
                ModelState.AddModelError(nameof(model.Email), "Email này đã được đăng ký.");
                return View(model);
            }

            var biTrungSoDienThoai = await _context.NguoiDungs
                .AnyAsync(u => u.SoDienThoai == soDienThoai);

            if (biTrungSoDienThoai)
            {
                ModelState.AddModelError(nameof(model.SoDienThoai), "Số điện thoại này đã được đăng ký.");
                return View(model);
            }

            // Tìm vai trò khách hàng; tạo vai trò nếu bảng VaiTro chưa có.
            var vaiTroKhachHang = await _context.VaiTros
                .FirstOrDefaultAsync(v =>
                    v.TenVaiTro == "Khách hàng" ||
                    v.TenVaiTro == "KhachHang");

            if (vaiTroKhachHang == null)
            {
                vaiTroKhachHang = new VaiTro { TenVaiTro = "Khách hàng" };
                _context.VaiTros.Add(vaiTroKhachHang);
                await _context.SaveChangesAsync();
            }

            var nguoiDungMoi = new NguoiDung
            {
                TenDangNhap = tenDangNhap,
                HoTen = model.HoTen.Trim(),
                Email = email,
                SoDienThoai = soDienThoai,
                TrangThai = true,
                MaVaiTro = vaiTroKhachHang.MaVaiTro
            };

            nguoiDungMoi.MatKhau = _passwordHasher.HashPassword(
                nguoiDungMoi,
                model.MatKhau);

            var khachHangMoi = new KhachHang
            {
                HoTen = nguoiDungMoi.HoTen,
                Email = email,
                SoDienThoai = soDienThoai,
                NgayTao = DateTime.Now,
                TrangThai = true
            };

            _context.NguoiDungs.Add(nguoiDungMoi);
            _context.KhachHangs.Add(khachHangMoi);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty,
                    "Thông tin tài khoản đã tồn tại. Hãy kiểm tra tên đăng nhập, email hoặc số điện thoại.");
                return View(model);
            }

            TempData["ThongBao"] = "Đăng ký thành công. Bạn có thể đăng nhập.";
            return RedirectToAction(nameof(Login));
        }
    }
}