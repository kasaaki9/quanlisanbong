using System.ComponentModel.DataAnnotations;

namespace QuanLySanBong.Models;

public class VaiTro
{
    [Key] public int MaVaiTro { get; set; }
    [Required, StringLength(50)] public string TenVaiTro { get; set; } = string.Empty;

    public ICollection<NguoiDung> NguoiDungs { get; set; } = new List<NguoiDung>();
}