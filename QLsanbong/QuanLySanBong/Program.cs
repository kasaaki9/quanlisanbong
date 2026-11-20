using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using QuanLySanBong.Models;
using QuanLySanBong.Data;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient("Ollama", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Ollama:BaseUrl"] ?? "http://localhost:11434/");
    client.Timeout = TimeSpan.FromMinutes(3);
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("support-chat", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
});

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var adminRole = await db.VaiTros.FirstOrDefaultAsync(role => role.TenVaiTro == "Admin");
    if (adminRole == null)
    {
        adminRole = new VaiTro { TenVaiTro = "Admin" };
        db.VaiTros.Add(adminRole);
        await db.SaveChangesAsync();
    }

    const string adminUsername = "admin";
    const string adminPassword = "Admin@123";
    var admin = await db.NguoiDungs.FirstOrDefaultAsync(user => user.TenDangNhap == adminUsername);
    var passwordHasher = new PasswordHasher<NguoiDung>();
    if (admin == null)
    {
        admin = new NguoiDung
        {
            TenDangNhap = adminUsername,
            HoTen = "Quản trị viên",
            TrangThai = true,
            MaVaiTro = adminRole.MaVaiTro
        };
        admin.MatKhau = passwordHasher.HashPassword(admin, adminPassword);
        db.NguoiDungs.Add(admin);
        await db.SaveChangesAsync();
    }
    else
    {
        admin.MaVaiTro = adminRole.MaVaiTro;
        admin.TrangThai = true;
        if (passwordHasher.VerifyHashedPassword(admin, admin.MatKhau, adminPassword) == PasswordVerificationResult.Failed)
            admin.MatKhau = passwordHasher.HashPassword(admin, adminPassword);
        await db.SaveChangesAsync();
    }
    foreach (var staffRoleName in new[] { "Nhân viên", "Nhân viên thu ngân" })
    {
        if (!await db.VaiTros.AnyAsync(role => role.TenVaiTro == staffRoleName))
            db.VaiTros.Add(new VaiTro { TenVaiTro = staffRoleName });
    }
    await db.SaveChangesAsync();
    foreach (var people in new[] { 5, 7, 11 })
    {
        if (!await db.LoaiSans.AnyAsync(type => type.SoLuongToiDa == people))
            db.LoaiSans.Add(new LoaiSan { TenLoaiSan = $"Sân {people} người", SoLuongToiDa = people });
    }
    await db.SaveChangesAsync();

    var fieldTypes = await db.LoaiSans.OrderBy(type => type.SoLuongToiDa).ToListAsync();
    foreach (var type in fieldTypes)
    {
        if (!await db.SanBongs.AnyAsync(field => field.MaLoaiSan == type.MaLoaiSan))
        {
            var players = type.SoLuongToiDa;
            var image = players switch
            {
                5 => "https://images.unsplash.com/photo-1522778119026-d647f0596c20?auto=format&fit=crop&w=1200&q=80",
                7 => "https://images.unsplash.com/photo-1518604666860-9ed391f76460?auto=format&fit=crop&w=1200&q=80",
                _ => "https://images.unsplash.com/photo-1729843352938-0e10fbf96585?auto=format&fit=crop&w=1200&q=80"
            };
            db.SanBongs.Add(new SanBong
            {
                TenSan = $"Sân {players} người ({(players == 5 ? "A1" : players == 7 ? "B1" : "C1")})",
                MaLoaiSan = type.MaLoaiSan,
                MoTa = "Sân cỏ nhân tạo, có đèn chiếu sáng buổi tối.",
                HinhAnh = image,
                TrangThai = "Hoạt động"
            });
        }
    }
    await db.SaveChangesAsync();

    var fieldsWithOldElevenAsideImages = await db.SanBongs
        .Where(field => field.HinhAnh != null &&
            (field.HinhAnh.Contains("photo-1508098682722-e99c43a406b2") || field.HinhAnh.Contains("photo-1546717003-caee5f93a9db")))
        .ToListAsync();
    foreach (var field in fieldsWithOldElevenAsideImages)
        field.HinhAnh = "https://images.unsplash.com/photo-1729843352938-0e10fbf96585?auto=format&fit=crop&w=1200&q=80";
    if (fieldsWithOldElevenAsideImages.Count > 0)
        await db.SaveChangesAsync();

    var fieldsWithoutPrices = await db.SanBongs.Include(field => field.BangGias)
        .Where(field => !field.BangGias.Any()).ToListAsync();
    foreach (var field in fieldsWithoutPrices)
    {
        var players = await db.LoaiSans.Where(type => type.MaLoaiSan == field.MaLoaiSan).Select(type => type.SoLuongToiDa).FirstAsync();
        var morning = players switch { 5 => 160000m, 7 => 280000m, _ => 550000m };
        var afternoon = players switch { 5 => 220000m, 7 => 350000m, _ => 700000m };
        var evening = players switch { 5 => 300000m, 7 => 450000m, _ => 900000m };
        db.BangGias.AddRange(
            new BangGia { MaSan = field.MaSan, GioBatDau = TimeSpan.FromHours(6), GioKetThuc = TimeSpan.FromHours(15), GiaTien = morning, LoaiNgay = "Tất cả" },
            new BangGia { MaSan = field.MaSan, GioBatDau = TimeSpan.FromHours(15), GioKetThuc = TimeSpan.FromHours(18), GiaTien = afternoon, LoaiNgay = "Tất cả" },
            new BangGia { MaSan = field.MaSan, GioBatDau = TimeSpan.FromHours(18), GioKetThuc = TimeSpan.FromHours(23), GiaTien = evening, LoaiNgay = "Tất cả" });
    }
    if (!await db.LoaiThanhToans.AnyAsync())
        db.LoaiThanhToans.AddRange(new LoaiThanhToan { TenLoaiTT = "Tiền mặt" }, new LoaiThanhToan { TenLoaiTT = "Chuyển khoản" }, new LoaiThanhToan { TenLoaiTT = "Thẻ" });
    await db.SaveChangesAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseRateLimiter();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();


