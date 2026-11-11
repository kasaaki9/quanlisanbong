using System.ComponentModel.DataAnnotations;

namespace QuanLySanBong.Models;

public class BangGia
{
    [Key] public int MaGia { get; set; }
    public int MaSan { get; set; }
    public TimeSpan GioBatDau { get; set; }
    public TimeSpan GioKetThuc { get; set; }
    public decimal GiaTien { get; set; }
    [StringLength(20)] public string? LoaiNgay { get; set; }

    public SanBong? SanBong { get; set; }
}