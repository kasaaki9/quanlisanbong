using System.ComponentModel.DataAnnotations;

namespace QuanLySanBong.Models;

public class DanhGia
{
    [Key] public int MaDanhGia { get; set; }
    public int MaPhieu { get; set; }
    public int MaNguoiDung { get; set; }
    public int SoSao { get; set; }
    [StringLength(500)] public string? NoiDung { get; set; }
    public DateTime NgayDanhGia { get; set; } = DateTime.Now;

    public PhieuDatSan? PhieuDatSan { get; set; }
    public NguoiDung? NguoiDung { get; set; }
}
