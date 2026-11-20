namespace QuanLySanBong.ViewModels;

public class SanBongListViewModel
{
    public int? SoNguoi { get; set; }
    public string? DiaDiem { get; set; }
    public List<string> DiaDiemOptions { get; set; } = new();
    public double? ViDoNguoiDung { get; set; }
    public double? KinhDoNguoiDung { get; set; }
    public List<SanBongCardViewModel> SanBongs { get; set; } = new();
}

public class SanBongCardViewModel
{
    public int MaSan { get; set; }
    public string TenSan { get; set; } = string.Empty;
    public string TenLoaiSan { get; set; } = string.Empty;
    public int SoNguoi { get; set; }
    public string? MoTa { get; set; }
    public string? DiaChi { get; set; }
    public string HinhAnh { get; set; } = string.Empty;
    public decimal? GiaTu { get; set; }
    public double? KhoangCachKm { get; set; }
}

public class SanBongDetailViewModel
{
    public int MaSan { get; set; }
    public string TenSan { get; set; } = string.Empty;
    public string TenLoaiSan { get; set; } = string.Empty;
    public int SoNguoi { get; set; }
    public string? MoTa { get; set; }
    public string? DiaChi { get; set; }
    public double? ViDo { get; set; }
    public double? KinhDo { get; set; }
    public string HinhAnh { get; set; } = string.Empty;
    public DateTime NgayChoi { get; set; }
    public decimal? GiaTu { get; set; }
    public double DiemTrungBinh { get; set; }
    public int SoDanhGia { get; set; }
    public List<SanBongSlotViewModel> KhungGios { get; set; } = new();
    public List<SanBongReviewViewModel> DanhGias { get; set; } = new();
}

public class SanBongSlotViewModel
{
    public TimeSpan BatDau { get; set; }
    public TimeSpan KetThuc { get; set; }
    public decimal GiaMoiGio { get; set; }
    public decimal TongTien { get; set; }
    public bool ConTrong { get; set; }
}

public class SanBongReviewViewModel
{
    public string TenNguoiDung { get; set; } = string.Empty;
    public int SoSao { get; set; }
    public string NoiDung { get; set; } = string.Empty;
    public DateTime NgayDanhGia { get; set; }
}

public class SanBongBookingOptionViewModel
{
    public int MaPhieu { get; set; }
    public DateTime NgayDat { get; set; }
}

public class DatSanThanhToanViewModel
{
    public int MaSan { get; set; }
    public string TenSan { get; set; } = string.Empty;
    public string TenLoaiSan { get; set; } = string.Empty;
    public string HinhAnh { get; set; } = string.Empty;
    public DateTime NgayDat { get; set; }
    public TimeSpan GioBatDau { get; set; }
    public TimeSpan GioKetThuc { get; set; }
    public decimal GiaMoiGio { get; set; }
    public decimal TongTien { get; set; }
    public string PhuongThuc { get; set; } = "Tiền mặt";
    public string? LoiLich { get; set; }
    public List<string> PhuongThucs { get; set; } = new();
}

public class DatSanHoanTatViewModel
{
    public int MaPhieu { get; set; }
    public string TenSan { get; set; } = string.Empty;
    public DateTime NgayDat { get; set; }
    public TimeSpan GioBatDau { get; set; }
    public TimeSpan GioKetThuc { get; set; }
    public decimal TongTien { get; set; }
    public string TrangThai { get; set; } = string.Empty;
    public string PhuongThuc { get; set; } = string.Empty;
}
