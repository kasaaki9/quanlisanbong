using System.Security.Claims;
using System.Text.RegularExpressions;
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
        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
                return View(model);

            var thongTinDangNhap = model.TenDangNhap.Trim().ToLower();

            var nguoiDung = await _context.NguoiDungs
                .Include(u => u.VaiTro)
                .FirstOrDefaultAsync(u =>
                    u.TenDangNhap.ToLower() == thongTinDangNhap ||
                    (u.Email != null && u.Email.ToLower() == thongTinDangNhap));

            if (nguoiDung == null)
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

            if (!nguoiDung.TrangThai)
            {
                ModelState.AddModelError(string.Empty, TaoThongBaoKhoa("Tài khoản", nguoiDung.LyDoKhoa));
                return View(model);
            }

            var roleName = nguoiDung.VaiTro?.TenVaiTro;
            if (roleName is "Nhân viên" or "Nhân viên thu ngân")
            {
                NhanVien? employee = null;
                if (!string.IsNullOrWhiteSpace(nguoiDung.Email))
                    employee = await _context.NhanViens.AsNoTracking().FirstOrDefaultAsync(x => x.Email == nguoiDung.Email);
                if (employee == null && !string.IsNullOrWhiteSpace(nguoiDung.SoDienThoai))
                    employee = await _context.NhanViens.AsNoTracking().FirstOrDefaultAsync(x => x.SoDienThoai == nguoiDung.SoDienThoai);
                if (employee == null)
                    employee = await _context.NhanViens.AsNoTracking().FirstOrDefaultAsync(x => x.HoTen == nguoiDung.HoTen);
                if (employee is { TrangThai: false })
                {
                    ModelState.AddModelError(string.Empty, TaoThongBaoKhoa("Hồ sơ nhân viên", employee.LyDoKhoa));
                    return View(model);
                }
            }
            else if (roleName is not ("Admin" or "Super Admin" or "Quản trị viên" or "Quản lý sân"))
            {
                var customer = await _context.KhachHangs.AsNoTracking().FirstOrDefaultAsync(x =>
                    (nguoiDung.Email != null && x.Email == nguoiDung.Email) ||
                    (nguoiDung.SoDienThoai != null && x.SoDienThoai == nguoiDung.SoDienThoai));
                if (customer is { TrangThai: false })
                {
                    ModelState.AddModelError(string.Empty, TaoThongBaoKhoa("Hồ sơ khách hàng", customer.LyDoKhoa));
                    return View(model);
                }
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

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);
            if (roleName is "Admin" or "Super Admin" or "Quản trị viên" or "Quản lý sân")
                return RedirectToAction("Index", "Admin");
            if (roleName is "Nhân viên" or "Nhân viên thu ngân")
                return RedirectToAction("Index", "NhanVien");
            return RedirectToAction(nameof(Profile));
        }

        private static string TaoThongBaoKhoa(string tenHoSo, string? lyDo)
        {
            return string.IsNullOrWhiteSpace(lyDo)
                ? $"{tenHoSo} của bạn đang bị khóa. Vui lòng liên hệ quản trị viên."
                : $"{tenHoSo} của bạn đang bị khóa. Lý do: {lyDo}";
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
        
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return RedirectToAction(nameof(Login));

            var account = await _context.NguoiDungs.AsNoTracking()
                .FirstOrDefaultAsync(user => user.MaNguoiDung == userId);
            if (account == null || !account.TrangThai)
            {
                await HttpContext.SignOutAsync("Cookies");
                return RedirectToAction(nameof(Login));
            }

            return View(new ProfileEditViewModel
            {
                HoTen = account.HoTen,
                TenDangNhap = account.TenDangNhap,
                Email = account.Email,
                SoDienThoai = account.SoDienThoai
            });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(ProfileEditViewModel model)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return RedirectToAction(nameof(Login));

            if (!ModelState.IsValid)
                return View(model);

            var account = await _context.NguoiDungs.Include(user => user.VaiTro)
                .FirstOrDefaultAsync(user => user.MaNguoiDung == userId);
            if (account == null || !account.TrangThai)
            {
                await HttpContext.SignOutAsync("Cookies");
                return RedirectToAction(nameof(Login));
            }

            var fullName = model.HoTen.Trim();
            var email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim().ToLowerInvariant();
            var phone = string.IsNullOrWhiteSpace(model.SoDienThoai) ? null : model.SoDienThoai.Trim();
            if (fullName.Length is < 2 or > 100)
                ModelState.AddModelError(nameof(model.HoTen), "Họ tên phải từ 2 đến 100 ký tự.");

            if (email != null && await _context.NguoiDungs.AnyAsync(user =>
                    user.MaNguoiDung != userId && user.Email != null && user.Email.ToLower() == email))
                ModelState.AddModelError(nameof(model.Email), "Email này đã được tài khoản khác sử dụng.");

            if (phone != null && await _context.NguoiDungs.AnyAsync(user =>
                    user.MaNguoiDung != userId && user.SoDienThoai == phone))
                ModelState.AddModelError(nameof(model.SoDienThoai), "Số điện thoại này đã được tài khoản khác sử dụng.");

            var passwordChangeRequested =
                !string.IsNullOrWhiteSpace(model.MatKhauHienTai) ||
                !string.IsNullOrWhiteSpace(model.MatKhauMoi) ||
                !string.IsNullOrWhiteSpace(model.XacNhanMatKhauMoi);

            if (passwordChangeRequested)
            {
                if (string.IsNullOrWhiteSpace(model.MatKhauHienTai))
                    ModelState.AddModelError(nameof(model.MatKhauHienTai), "Nhập mật khẩu hiện tại để xác nhận đổi mật khẩu.");
                else if (_passwordHasher.VerifyHashedPassword(account, account.MatKhau, model.MatKhauHienTai) == PasswordVerificationResult.Failed)
                    ModelState.AddModelError(nameof(model.MatKhauHienTai), "Mật khẩu hiện tại không đúng.");

                if (string.IsNullOrWhiteSpace(model.MatKhauMoi))
                    ModelState.AddModelError(nameof(model.MatKhauMoi), "Nhập mật khẩu mới.");
                else if (model.MatKhauMoi.Length < 8 || model.MatKhauMoi.Length > 64)
                    ModelState.AddModelError(nameof(model.MatKhauMoi), "Mật khẩu mới phải từ 8 đến 64 ký tự.");
                else if (!Regex.IsMatch(model.MatKhauMoi, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z\d]).+$"))
                    ModelState.AddModelError(nameof(model.MatKhauMoi), "Mật khẩu cần có chữ hoa, chữ thường, số và ký tự đặc biệt.");
            }

            if (!ModelState.IsValid)
                return View(model);

            var oldEmail = account.Email;
            var oldPhone = account.SoDienThoai;
            var roleName = account.VaiTro?.TenVaiTro;
            KhachHang? customerProfile = null;
            NhanVien? employeeProfile = null;

            if (roleName is "Nhân viên" or "Nhân viên thu ngân")
            {
                if (!string.IsNullOrWhiteSpace(oldEmail))
                    employeeProfile = await _context.NhanViens.FirstOrDefaultAsync(employee => employee.Email == oldEmail);
                if (employeeProfile == null && !string.IsNullOrWhiteSpace(oldPhone))
                    employeeProfile = await _context.NhanViens.FirstOrDefaultAsync(employee => employee.SoDienThoai == oldPhone);
            }
            else if (roleName is not ("Admin" or "Super Admin" or "Quản trị viên" or "Quản lý sân"))
            {
                if (!string.IsNullOrWhiteSpace(oldEmail))
                    customerProfile = await _context.KhachHangs.FirstOrDefaultAsync(customer => customer.Email == oldEmail);
                if (customerProfile == null && !string.IsNullOrWhiteSpace(oldPhone))
                    customerProfile = await _context.KhachHangs.FirstOrDefaultAsync(customer => customer.SoDienThoai == oldPhone);
            }

            account.HoTen = fullName;
            account.Email = email;
            account.SoDienThoai = phone;
            if (passwordChangeRequested)
                account.MatKhau = _passwordHasher.HashPassword(account, model.MatKhauMoi!);

            if (customerProfile != null)
            {
                customerProfile.HoTen = fullName;
                customerProfile.Email = email;
                customerProfile.SoDienThoai = phone;
            }
            if (employeeProfile != null)
            {
                employeeProfile.HoTen = fullName;
                employeeProfile.Email = email;
                employeeProfile.SoDienThoai = phone;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Không thể lưu thay đổi. Hãy kiểm tra email hoặc số điện thoại rồi thử lại.");
                return View(model);
            }

            var authentication = await HttpContext.AuthenticateAsync("Cookies");
            if (authentication.Principal?.Identity is ClaimsIdentity identity)
            {
                foreach (var oldNameClaim in identity.FindAll(ClaimTypes.Name).ToList())
                    identity.RemoveClaim(oldNameClaim);
                identity.AddClaim(new Claim(ClaimTypes.Name, fullName));
                await HttpContext.SignInAsync("Cookies", authentication.Principal, authentication.Properties);
            }

            TempData["ProfileSuccess"] = passwordChangeRequested
                ? "Đã cập nhật thông tin và đổi mật khẩu thành công."
                : "Đã cập nhật thông tin tài khoản.";
            return RedirectToAction(nameof(Profile));
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

        [HttpGet]
        public IActionResult Logout() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Logout")]
        public async Task<IActionResult> LogoutPost()
        {
            await HttpContext.SignOutAsync("Cookies");
            return RedirectToAction("Index", "Home");
        }
    }
}