using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLySanBong.Models;
using QuanLySanBong.ViewModels;

namespace QuanLySanBong.Controllers;


public partial class AdminController
{
    [HttpGet]
[Authorize(Roles = "Admin,Super Admin,Quản trị viên,Quản lý sân")]
    public async Task<IActionResult> Sua(string id, int key)
    {
        id = id?.ToLowerInvariant() ?? "";
        if (id == "khach-hang") return RejectCustomerManagement();
        if (!CreateEntities.TryGetValue(id, out var config)) return NotFound();
        var entity = await _db.FindAsync(config.Entity, new object?[] { key });
        if (entity == null) return NotFound();

        var model = new AdminCreateViewModel { Id = id, Title = config.Title, IsEdit = true, RecordId = key };
        await FillCreateFields(model);
        foreach (var field in model.Fields)
        {
            var property = config.Entity.GetProperty(field.Name);
            var value = property?.GetValue(entity);
            if (value == null || field.Type == "password" || field.Type == "file") continue;
            field.Value = value switch
            {
                DateTime date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                TimeSpan time => time.ToString(@"hh\:mm", CultureInfo.InvariantCulture),
                IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
                _ => value.ToString()
            };
        }
        await SetSidebarCounts();
        ViewBag.ActiveModule = id;
        ViewData["Title"] = "S\u1eeda " + config.Title;
        return View("TaoMoi", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
[Authorize(Roles = "Admin,Super Admin,Quản trị viên,Quản lý sân")]
    public async Task<IActionResult> Sua(string id, int key, IFormCollection form, IFormFile? hinhAnh)
    {
        id = id?.ToLowerInvariant() ?? "";
        if (id == "khach-hang") return RejectCustomerManagement();
        if (!CreateEntities.TryGetValue(id, out var config)) return NotFound();
        var entity = await _db.FindAsync(config.Entity, new object?[] { key });
        if (entity == null) return NotFound();

        var schema = new AdminCreateViewModel { Id = id, Title = config.Title, IsEdit = true, RecordId = key };
        await FillCreateFields(schema);
        var allowed = schema.Fields.Select(field => field.Name).ToHashSet(StringComparer.Ordinal);
        foreach (var property in config.Entity.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!allowed.Contains(property.Name) || property.SetMethod == null || property.PropertyType == typeof(string) && property.Name == "HinhAnh") continue;
            if (!form.TryGetValue(property.Name, out var raw)) continue;
            if (property.Name == "MatKhau" && string.IsNullOrWhiteSpace(raw)) continue;
            try
            {
                var target = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                object value = target == typeof(string) ? raw.ToString().Trim()
                    : target == typeof(TimeSpan) ? TimeSpan.Parse(raw.ToString(), CultureInfo.InvariantCulture)
                    : target == typeof(DateTime) ? DateTime.Parse(raw.ToString(), CultureInfo.InvariantCulture)
                    : target == typeof(bool) ? raw.ToString().Split(',').Contains("true", StringComparer.OrdinalIgnoreCase)
                    : Convert.ChangeType(raw.ToString(), target, CultureInfo.InvariantCulture)!;
                if (property.Name == "MatKhau" && entity is NguoiDung user)
                    value = new PasswordHasher<NguoiDung>().HashPassword(user, (string)value);
                property.SetValue(entity, value);
            }
            catch (Exception) when (property.PropertyType != typeof(string))
            {
                ModelState.AddModelError(property.Name, "Gi\u00e1 tr\u1ecb kh\u00f4ng h\u1ee3p l\u1ec7.");
            }
        }

        if (entity is SanBong court && hinhAnh is { Length: > 0 })
        {
            var extension = Path.GetExtension(hinhAnh.FileName).ToLowerInvariant();
            if (!new[] { ".jpg", ".jpeg", ".png", ".webp" }.Contains(extension) || hinhAnh.Length > 5 * 1024 * 1024)
                ModelState.AddModelError("hinhAnh", "\u1ea2nh ph\u1ea3i l\u00e0 JPG, PNG ho\u1eb7c WebP v\u00e0 kh\u00f4ng qu\u00e1 5 MB.");
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
            foreach (var error in validation) ModelState.AddModelError("", error.ErrorMessage ?? "D\u1eef li\u1ec7u ch\u01b0a h\u1ee3p l\u1ec7.");
        if (!ModelState.IsValid)
        {
            foreach (var field in schema.Fields)
                if (field.Type != "password" && form.TryGetValue(field.Name, out var value)) field.Value = value.ToString();
            await SetSidebarCounts();
            ViewBag.ActiveModule = id;
            return View("TaoMoi", schema);
        }

        try
        {
            await _db.SaveChangesAsync();
            TempData["AdminSuccess"] = "\u0110\u00e3 l\u01b0u thay \u0111\u1ed5i.";
            return RedirectToAction(nameof(Module), new { id });
        }
        catch (DbUpdateException)
        {
            TempData["AdminError"] = "Kh\u00f4ng l\u01b0u \u0111\u01b0\u1ee3c. H\u00e3y ki\u1ec3m tra d\u1eef li\u1ec7u li\u00ean quan.";
            return RedirectToAction(nameof(Module), new { id });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
[Authorize(Roles = "Admin,Super Admin,Quản trị viên,Quản lý sân")]
    public async Task<IActionResult> DoiTrangThai(string id, int key, string? lyDoKhoa)
    {
        id = id?.ToLowerInvariant() ?? "";
        if (id == "khach-hang") return RejectCustomerManagement();
        var currentUserId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var parsedUserId) ? parsedUserId : 0;
        var reason = lyDoKhoa?.Trim();
        if (reason?.Length > 500)
        {
            TempData["AdminError"] = "Lý do khóa không được vượt quá 500 ký tự.";
            return RedirectToAction(nameof(Module), new { id });
        }

        string tenBanGhi;
        bool dangHoatDong;
        switch (id)
        {
            case "nguoi-dung":
            {
                var account = await _db.NguoiDungs.FindAsync(key);
                if (account == null) return NotFound();
                if (account.MaNguoiDung == currentUserId && account.TrangThai)
                {
                    TempData["AdminError"] = "Bạn không thể tự khóa tài khoản đang đăng nhập.";
                    return RedirectToAction(nameof(Module), new { id });
                }
                if (account.TrangThai && string.IsNullOrWhiteSpace(reason))
                {
                    TempData["AdminError"] = "Vui lòng nhập lý do khóa tài khoản.";
                    return RedirectToAction(nameof(Module), new { id });
                }
                account.TrangThai = !account.TrangThai;
                account.LyDoKhoa = account.TrangThai ? null : reason;
                tenBanGhi = "Tài khoản";
                dangHoatDong = account.TrangThai;
                break;
            }
            case "khach-hang":
            {
                var customer = await _db.KhachHangs.FindAsync(key);
                if (customer == null) return NotFound();
                if (customer.TrangThai && string.IsNullOrWhiteSpace(reason))
                {
                    TempData["AdminError"] = "Vui lòng nhập lý do khóa khách hàng.";
                    return RedirectToAction(nameof(Module), new { id });
                }
                customer.TrangThai = !customer.TrangThai;
                customer.LyDoKhoa = customer.TrangThai ? null : reason;
                tenBanGhi = "Khách hàng";
                dangHoatDong = customer.TrangThai;
                break;
            }
            case "nhan-vien":
            {
                var employee = await _db.NhanViens.FindAsync(key);
                if (employee == null) return NotFound();
                if (employee.TrangThai && string.IsNullOrWhiteSpace(reason))
                {
                    TempData["AdminError"] = "Vui lòng nhập lý do khóa nhân viên.";
                    return RedirectToAction(nameof(Module), new { id });
                }
                employee.TrangThai = !employee.TrangThai;
                employee.LyDoKhoa = employee.TrangThai ? null : reason;
                tenBanGhi = "Nhân viên";
                dangHoatDong = employee.TrangThai;
                break;
            }
            default:
                return NotFound();
        }

        await _db.SaveChangesAsync();
        TempData["AdminSuccess"] = $"Đã {(dangHoatDong ? "mở khóa" : "khóa")} {tenBanGhi.ToLowerInvariant()}.";
        return RedirectToAction(nameof(Module), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
[Authorize(Roles = "Admin,Super Admin,Quản trị viên,Quản lý sân")]
    public async Task<IActionResult> Xoa(string id, int key)
    {
        id = id?.ToLowerInvariant() ?? "";
        if (id == "khach-hang") return RejectCustomerManagement();
        if (!CreateEntities.TryGetValue(id, out var config)) return NotFound();
        var entity = await _db.FindAsync(config.Entity, new object?[] { key });
        if (entity == null) return NotFound();
        _db.Remove(entity);
        try
        {
            await _db.SaveChangesAsync();
            TempData["AdminSuccess"] = "\u0110\u00e3 x\u00f3a b\u1ea3n ghi.";
        }
        catch (DbUpdateException)
        {
            TempData["AdminError"] = "B\u1ea3n ghi \u0111ang \u0111\u01b0\u1ee3c s\u1eed d\u1ee5ng n\u00ean kh\u00f4ng th\u1ec3 x\u00f3a.";
        }
        return RedirectToAction(nameof(Module), new { id });
    }
}
