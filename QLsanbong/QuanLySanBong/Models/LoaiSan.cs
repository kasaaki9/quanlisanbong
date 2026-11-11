using System.ComponentModel.DataAnnotations;

namespace QuanLySanBong.Models;

public class LoaiSan
{
    [Key] public int MaLoaiSan { get; set; }
    [Required, StringLength(50)] public string TenLoaiSan { get; set; } = string.Empty;
    public int SoLuongToiDa { get; set; }

    public ICollection<SanBong> SanBongs { get; set; } = new List<SanBong>();
}