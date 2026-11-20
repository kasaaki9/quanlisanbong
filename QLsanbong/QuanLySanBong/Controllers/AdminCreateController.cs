using System.Globalization;
using System.Reflection;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuanLySanBong.Data;
using QuanLySanBong.Models;
using QuanLySanBong.ViewModels;

namespace QuanLySanBong.Controllers;

[Authorize(Roles = "Admin,Super Admin,Quản trị viên,Quản lý sân")]
public partial class AdminController
{
    private static readonly Dictionary<string, (string Title, Type Entity)> CreateEntities = new()
    {
        ["loai-san"] = ("Loại sân", typeof(LoaiSan)), ["san-bong"] = ("Sân bóng", typeof(SanBong)),
        ["bang-gia"] = ("Bảng giá", typeof(BangGia)), ["khach-hang"] = ("Khách hàng", typeof(KhachHang)),
        ["phieu-dat-san"] = ("Phiếu đặt sân", typeof(PhieuDatSan)), ["dich-vu"] = ("Dịch vụ", typeof(DichVu)),
        ["loai-thanh-toan"] = ("Loại thanh toán", typeof(LoaiThanhToan)), ["nhan-vien"] = ("Nhân viên", typeof(NhanVien)),
        ["ca-lam-viec"] = ("Ca làm việc", typeof(CaLamViec)), ["lich-lam"] = ("Lịch làm", typeof(LichLam)),
        ["thong-bao"] = ("Thông báo", typeof(ThongBao)), ["ho-tro"] = ("Yêu cầu hỗ trợ", typeof(HoTro)),
        ["nguoi-dung"] = ("Người dùng", typeof(NguoiDung)), ["vai-tro"] = ("Vai trò", typeof(VaiTro))
    };
    [HttpGet]
    public async Task<IActionResult> TaoMoi(string id)
    {
        id = id?.ToLowerInvariant() ?? "";
        if (!CreateEntities.TryGetValue(id, out var config)) return NotFound();
        var vm = new AdminCreateViewModel { Id = id, Title = config.Title };
        await FillCreateFields(vm);
        await SetSidebarCounts();
        ViewBag.ActiveModule = id;
        ViewData["Title"] = "Thêm " + config.Title;
        return View("TaoMoi", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TaoMoi(string id, IFormCollection form, IFormFile? hinhAnh)
    {
        id = id?.ToLowerInvariant() ?? "";
        if (!CreateEntities.TryGetValue(id, out var config)) return NotFound();
        var fieldSchema = new AdminCreateViewModel { Id = id, Title = config.Title };
        await FillCreateFields(fieldSchema);
        var allowedFields = fieldSchema.Fields.Select(field => field.Name).ToHashSet(StringComparer.Ordinal);
        var entity = Activator.CreateInstance(config.Entity)!;
        foreach (var property in config.Entity.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!allowedFields.Contains(property.Name)) continue;
            if (!form.TryGetValue(property.Name, out var raw) || property.SetMethod == null || string.IsNullOrWhiteSpace(raw)) continue;
            try
            {
                var target = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                object value = target == typeof(string) ? raw.ToString().Trim()
                    : target == typeof(TimeSpan) ? TimeSpan.Parse(raw!, CultureInfo.InvariantCulture)
                    : target == typeof(DateTime) ? DateTime.Parse(raw!, CultureInfo.InvariantCulture)
                    : target == typeof(bool) ? raw.ToString().Split(',').Contains("true", StringComparer.OrdinalIgnoreCase)
                    : Convert.ChangeType(raw.ToString(), target, CultureInfo.InvariantCulture)!;
                property.SetValue(entity, value);
            }
            catch (Exception) when (property.PropertyType != typeof(string))
            {
                ModelState.AddModelError(property.Name, $"Giá trị {property.Name} không hợp lệ.");
            }
        }

        if (entity is NguoiDung user)
        {
            var password = form["MatKhau"].ToString();
            if (password.Length < 8) ModelState.AddModelError("MatKhau", "Mật khẩu cần ít nhất 8 ký tự.");
            else user.MatKhau = new PasswordHasher<NguoiDung>().HashPassword(user, password);
        }
        if (entity is SanBong court && hinhAnh is { Length: > 0 })
        {
            var extension = Path.GetExtension(hinhAnh.FileName).ToLowerInvariant();
            if (!new[] { ".jpg", ".jpeg", ".png", ".webp" }.Contains(extension) || hinhAnh.Length > 5 * 1024 * 1024)
                ModelState.AddModelError("hinhAnh", "Ảnh cần là JPG, PNG hoặc WebP và không quá 5 MB.");
            else
            {
                var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "san-bong");
                Directory.CreateDirectory(folder);
                var fileName = $"{Guid.NewGuid():N}{extension}";
                await using var stream = System.IO.File.Create(Path.Combine(folder, fileName));
                await hinhAnh.CopyToAsync(stream);
                court.HinhAnh = "/uploads/san-bong/" + fileName;
            }
        }

        var validation = new List<ValidationResult>();
        if (!Validator.TryValidateObject(entity, new ValidationContext(entity), validation, true))
            foreach (var error in validation)
                ModelState.AddModelError("", error.ErrorMessage ?? "Dữ liệu chưa hợp lệ.");

        if (!ModelState.IsValid)
        {
            var vm = new AdminCreateViewModel { Id = id, Title = config.Title };
            await FillCreateFields(vm);
            foreach (var field in vm.Fields)
                if (form.TryGetValue(field.Name, out var value)) field.Value = value.ToString();
            await SetSidebarCounts();
            ViewBag.ActiveModule = id;
            ViewData["Title"] = "Thêm " + config.Title;
            return View("TaoMoi", vm);
        }

        try
        {
            _db.Add(entity);
            await _db.SaveChangesAsync();
            TempData["AdminSuccess"] = $"Đã thêm {config.Title.ToLowerInvariant()} thành công.";
            return RedirectToAction(nameof(Module), new { id });
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError("", "Không thể lưu bản ghi. Hãy kiểm tra các mục liên quan và thử lại.");
            var vm = new AdminCreateViewModel { Id = id, Title = config.Title };
            await FillCreateFields(vm);
            await SetSidebarCounts();
            ViewBag.ActiveModule = id;
            return View("TaoMoi", vm);
        }
    }

    private async Task FillCreateFields(AdminCreateViewModel model)
    {
        void Add(string name, string label, string type = "text", bool required = false, List<(string Value, string Text)>? options = null)
            => model.Fields.Add(new AdminCreateField { Name = name, Label = label, Type = type, Required = required, Options = options ?? new() });
        List<(string Value, string Text)> Options<T>(IEnumerable<T> items, Func<T, string> key, Func<T, string> label)
            => items.Select(item => (key(item), label(item))).ToList();

        switch (model.Id)
        {
            case "loai-san": Add("TenLoaiSan", "Tên loại sân", required: true); Add("SoLuongToiDa", "Số người tối đa", "number", true); break;
            case "san-bong":
                Add("TenSan", "Tên sân", required: true);
                Add("MaLoaiSan", "Loại sân", "select", true, Options(await _db.LoaiSans.OrderBy(x => x.SoLuongToiDa).ToListAsync(), x => x.MaLoaiSan.ToString(), x => x.TenLoaiSan));
                Add("MoTa", "Mô tả", "textarea"); Add("TrangThai", "Trạng thái", "select", true, new() { ("Hoạt động", "Hoạt động"), ("Tạm ngưng", "Tạm ngưng") });
                Add("DiaChi", "Địa chỉ"); Add("ViDo", "Vĩ độ", "number"); Add("KinhDo", "Kinh độ", "number"); Add("HinhAnh", "Ảnh sân", "file"); break;
            case "bang-gia":
                Add("MaSan", "Sân bóng", "select", true, Options(await _db.SanBongs.ToListAsync(), x => x.MaSan.ToString(), x => x.TenSan));
                Add("GioBatDau", "Giờ bắt đầu", "time", true); Add("GioKetThuc", "Giờ kết thúc", "time", true); Add("GiaTien", "Giá tiền", "number", true); Add("LoaiNgay", "Loại ngày"); break;
            case "khach-hang": Add("HoTen", "Họ tên", required: true); Add("SoDienThoai", "Số điện thoại", "tel"); Add("Email", "Email", "email"); Add("DiaChi", "Địa chỉ"); break;
            case "phieu-dat-san":
                Add("MaKhachHang", "Khách hàng", "select", true, Options(await _db.KhachHangs.ToListAsync(), x => x.MaKhachHang.ToString(), x => x.HoTen));
                Add("MaSan", "Sân bóng", "select", true, Options(await _db.SanBongs.ToListAsync(), x => x.MaSan.ToString(), x => x.TenSan));
                Add("NgayDat", "Ngày đặt", "date", true); Add("GioBatDau", "Giờ bắt đầu", "time", true); Add("GioKetThuc", "Giờ kết thúc", "time", true); Add("TongTienSan", "Tiền sân", "number", true); Add("GhiChu", "Ghi chú", "textarea"); break;
            case "dich-vu": Add("TenDichVu", "Tên dịch vụ", required: true); Add("DonGia", "Đơn giá", "number", true); Add("DonViTinh", "Đơn vị tính"); Add("SoLuongTon", "Số lượng tồn", "number", true); break;
            case "loai-thanh-toan": Add("TenLoaiTT", "Tên phương thức", required: true); break;
            case "nhan-vien": Add("HoTen", "Họ tên", required: true); Add("SoDienThoai", "Số điện thoại", "tel"); Add("Email", "Email", "email"); Add("ChucVu", "Chức vụ"); Add("TrangThai", "Đang làm việc", "checkbox"); break;
            case "ca-lam-viec":
                Add("TenCa", "Tên ca", required: true); Add("ThoiGianBatDau", "Giờ bắt đầu", "time", true); Add("ThoiGianKetThuc", "Giờ kết thúc", "time", true); Add("ThuTrongTuan", "Thứ trong tuần");
                Add("MaNhanVien", "Nhân viên", "select", true, Options(await _db.NhanViens.ToListAsync(), x => x.MaNhanVien.ToString(), x => x.HoTen)); break;
            case "lich-lam":
                Add("MaNhanVien", "Nhân viên", "select", true, Options(await _db.NhanViens.ToListAsync(), x => x.MaNhanVien.ToString(), x => x.HoTen)); Add("MaCa", "Ca làm", "select", true, Options(await _db.CaLamViecs.ToListAsync(), x => x.MaCa.ToString(), x => x.TenCa)); Add("NgayLam", "Ngày làm", "date", true); break;
            case "thong-bao": Add("MaNguoiDung", "Người nhận", "select", true, Options(await _db.NguoiDungs.ToListAsync(), x => x.MaNguoiDung.ToString(), x => x.HoTen)); Add("NoiDung", "Nội dung", "textarea", true); break;
            case "ho-tro": Add("TieuDe", "Tiêu đề", required: true); Add("NoiDung", "Nội dung", "textarea"); Add("MaNguoiDung", "Khách hàng", "select", true, Options(await _db.NguoiDungs.ToListAsync(), x => x.MaNguoiDung.ToString(), x => x.HoTen)); break;
            case "nguoi-dung": Add("TenDangNhap", "Tên đăng nhập", required: true); Add("MatKhau", "Mật khẩu", "password", !model.IsEdit); Add("HoTen", "Họ tên", required: true); Add("SoDienThoai", "Số điện thoại", "tel"); Add("Email", "Email", "email"); Add("MaVaiTro", "Vai trò", "select", true, Options(await _db.VaiTros.ToListAsync(), x => x.MaVaiTro.ToString(), x => x.TenVaiTro)); break;
            case "vai-tro": Add("TenVaiTro", "Tên vai trò", required: true); break;
        }
    }
}
