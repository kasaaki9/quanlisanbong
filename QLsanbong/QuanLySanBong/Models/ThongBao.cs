using System.ComponentModel.DataAnnotations;

namespace QuanLySanBong.Models;

public class ThongBao
{
    [Key] public int MaThongBao { get; set; }
    public int MaNguoiDung { get; set; }
    [Required, StringLength(500)] public string NoiDung { get; set; } = string.Empty;
    public DateTime NgayGui { get; set; } = DateTime.Now;
    public bool TrangThai { get; set; }

    public NguoiDung? NguoiDung { get; set; }
}
