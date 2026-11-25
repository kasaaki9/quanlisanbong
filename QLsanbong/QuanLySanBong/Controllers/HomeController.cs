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
            .OrderByDescending(s => s.MaSan)
            .Select(s => new SanBongHomeViewModel
            {
                MaSan = s.MaSan,
                TenSan = s.TenSan,
                TenLoaiSan = s.LoaiSan != null ? s.LoaiSan.TenLoaiSan : null,
                SoLuongToiDa = s.LoaiSan != null ? s.LoaiSan.SoLuongToiDa : 0,
                DiaChi = s.DiaChi,
                HinhAnh = s.HinhAnh,
                GiaTu = s.BangGias
                    .Select(g => (decimal?)g.GiaTien)
                    .Min()
            })
            .ToListAsync();

        foreach (var san in sanBongs.Where(s => string.IsNullOrWhiteSpace(s.HinhAnh)))
        {
            san.HinhAnh = san.SoLuongToiDa switch
            {
                5 => "https://c78484aff5.clvaw-cdnwnd.com/4b081b0c4653a1cbcadbab59acdb4e2e/200000109-5fe2b5fe2d/00003_MG_8351-2.jpg?ph=c78484aff5",
                7 => "https://images.squarespace-cdn.com/content/v1/60f8ea90e31fa85159faf2a5/e158d86c-4176-4f8c-8562-1b5fac3cc8ed/7aside2.jpg",
                _ => "https://images.unsplash.com/photo-1729843352938-0e10fbf96585?auto=format&fit=crop&w=1200&q=80"
            };
        }

        return View(new HomeIndexViewModel { SanBongs = sanBongs });
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