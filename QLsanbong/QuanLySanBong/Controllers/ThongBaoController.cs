using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLySanBong.Data;
using QuanLySanBong.Models;

namespace QuanLySanBong.Controllers;

[Authorize]
public class ThongBaoController : Controller
{
    private readonly AppDbContext _db;
    public ThongBaoController(AppDbContext db) => _db = db;

    private int? CurrentUserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    [HttpGet]
    public async Task<IActionResult> Recent()
    {
        var userId = CurrentUserId;
        if (userId == null) return Unauthorized();
        var unreadCount = await _db.ThongBaos.CountAsync(x => x.MaNguoiDung == userId.Value && !x.TrangThai);
        var recentItems = await _db.ThongBaos.AsNoTracking()
            .Where(x => x.MaNguoiDung == userId.Value)
            .OrderByDescending(x => x.NgayGui)
            .Take(5)
            .Select(x => new { x.NoiDung, x.NgayGui, x.TrangThai })
            .ToListAsync();
        var items = recentItems.Select(x => new { x.NoiDung, NgayGui = x.NgayGui.ToString("dd/MM/yyyy HH:mm"), x.TrangThai });
        return Json(new { unreadCount, items });
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = CurrentUserId;
        if (userId == null) return Challenge();
        var notifications = await _db.ThongBaos.AsNoTracking()
            .Where(x => x.MaNguoiDung == userId.Value)
            .OrderByDescending(x => x.NgayGui)
            .ToListAsync();
        ViewBag.UnreadCount = notifications.Count(x => !x.TrangThai);
        ViewData["Title"] = "Thông báo của tôi";
        return View(notifications);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = CurrentUserId;
        if (userId == null) return Challenge();
        var unread = await _db.ThongBaos.Where(x => x.MaNguoiDung == userId.Value && !x.TrangThai).ToListAsync();
        foreach (var notification in unread) notification.TrangThai = true;
        if (unread.Count > 0) await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}