using System.ComponentModel.DataAnnotations;

namespace QuanLySanBong.Models;

public class LichLam
{
    [Key] public int MaLich { get; set; }
    public int MaNhanVien { get; set; }
    public int MaCa { get; set; }
    public DateTime NgayLam { get; set; }
    public bool TrangThai { get; set; } = true;

    public NhanVien? NhanVien { get; set; }
    public CaLamViec? CaLamViec { get; set; }
}