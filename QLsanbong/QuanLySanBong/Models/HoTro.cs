using System.ComponentModel.DataAnnotations;

namespace QuanLySanBong.Models;

public class HoTro
{
    [Key] public int MaHoTro { get; set; }
    [Required, StringLength(100)] public string TieuDe { get; set; } = string.Empty;
    [StringLength(500)] public string? NoiDung { get; set; }
    [StringLength(50)] public string TrangThai { get; set; } = "Chưa xử lý";
    public DateTime NgayTao { get; set; } = DateTime.Now;
    public int MaNguoiDung { get; set; }

    public NguoiDung? NguoiDung { get; set; }
}