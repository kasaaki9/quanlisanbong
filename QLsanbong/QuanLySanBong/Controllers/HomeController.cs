using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLySanBong.Data;
using QuanLySanBong.Models;
using QuanLySanBong.ViewModels;

namespace QuanLySanBong.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _context;

    public HomeController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var sanBongs = await _context.SanBongs
            .AsNoTracking()
            .Where(s => s.TrangThai == "Hoạt động")
            .Select(s => new SanBongHomeViewModel
            {
                MaSan = s.MaSan,
                TenSan = s.TenSan,
                TenLoaiSan = s.LoaiSan != null ? s.LoaiSan.TenLoaiSan : null,
                SoLuongToiDa = s.LoaiSan != null ? s.LoaiSan.SoLuongToiDa : 0,
                MoTa = s.MoTa,
                HinhAnh = s.HinhAnh,
                GiaTu = s.BangGias
                    .Select(g => (decimal?)g.GiaTien)
                    .Min()
            })
            .ToListAsync();

        var model = new HomeIndexViewModel
        {
            NhomSan = new List<LoaiSanHomeViewModel>
            {
                new()
                {
                    SoNguoi = 5,
                    TenNhom = "Sân 5 người",
                    AnhMacDinh = "https://c78484aff5.clvaw-cdnwnd.com/4b081b0c4653a1cbcadbab59acdb4e2e/200000109-5fe2b5fe2d/00003_MG_8351-2.jpg?ph=c78484aff5",
                    SanBongs = sanBongs.Where(s => s.SoLuongToiDa == 5).ToList()
                },
                new()
                {
                    SoNguoi = 7,
                    TenNhom = "Sân 7 người",
                    AnhMacDinh = "https://images.squarespace-cdn.com/content/v1/60f8ea90e31fa85159faf2a5/e158d86c-4176-4f8c-8562-1b5fac3cc8ed/7aside2.jpg",
                    SanBongs = sanBongs.Where(s => s.SoLuongToiDa == 7).ToList()
                },
                new()
                {
                    SoNguoi = 11,
                    TenNhom = "Sân 11 người",
                    AnhMacDinh = "https://images.unsplash.com/photo-1546717003-caee5f93a9db?fm=jpg&ixlib=rb-4.1.0&q=60&w=1200",
                    SanBongs = sanBongs.Where(s => s.SoLuongToiDa == 11).ToList()
                }
            }
        };

        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }
}