    using Microsoft.EntityFrameworkCore;
    using QuanLySanBong.Models;

    namespace QuanLySanBong.Data;

    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<VaiTro> VaiTros => Set<VaiTro>();
        public DbSet<NguoiDung> NguoiDungs => Set<NguoiDung>();
        public DbSet<LoaiSan> LoaiSans => Set<LoaiSan>();
        public DbSet<SanBong> SanBongs => Set<SanBong>();
        public DbSet<BangGia> BangGias => Set<BangGia>();
        public DbSet<PhieuDatSan> PhieuDatSans => Set<PhieuDatSan>();
        public DbSet<DichVu> DichVus => Set<DichVu>();
        public DbSet<HoaDon> HoaDons => Set<HoaDon>();
        public DbSet<ChiTietHoaDon> ChiTietHoaDons => Set<ChiTietHoaDon>();
        public DbSet<LichSu> LichSus => Set<LichSu>();
        public DbSet<KhachHang> KhachHangs => Set<KhachHang>();
        public DbSet<ThongBao> ThongBaos => Set<ThongBao>();
        public DbSet<LoaiThanhToan> LoaiThanhToans => Set<LoaiThanhToan>();
        public DbSet<NhanVien> NhanViens => Set<NhanVien>();
        public DbSet<CaLamViec> CaLamViecs => Set<CaLamViec>();
        public DbSet<LichLam> LichLams => Set<LichLam>();
        public DbSet<DanhGia> DanhGias => Set<DanhGia>();
        public DbSet<HoTro> HoTros => Set<HoTro>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<NguoiDung>()
                .HasIndex(x => x.TenDangNhap)
                .IsUnique();

            modelBuilder.Entity<NguoiDung>()
                .HasOne(x => x.VaiTro)
                .WithMany(x => x.NguoiDungs)
                .HasForeignKey(x => x.MaVaiTro)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LichSu>()
                .HasOne(x => x.NguoiDung)
                .WithMany(x => x.LichSus)
                .HasForeignKey(x => x.MaNguoiDung)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LichSu>()
                .HasOne(x => x.LoaiThanhToan)
                .WithMany(x => x.LichSus)
                .HasForeignKey(x => x.MaLoaiTT)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ThongBao>()
                .HasOne(x => x.NguoiDung)
                .WithMany(x => x.ThongBaos)
                .HasForeignKey(x => x.MaNguoiDung)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<HoTro>()
                .HasOne(x => x.NguoiDung)
                .WithMany(x => x.HoTros)
                .HasForeignKey(x => x.MaNguoiDung)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DanhGia>()
                .HasOne(x => x.NguoiDung)
                .WithMany(x => x.DanhGias)
                .HasForeignKey(x => x.MaNguoiDung)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DanhGia>()
                .HasOne(x => x.PhieuDatSan)
                .WithMany(x => x.DanhGias)
                .HasForeignKey(x => x.MaPhieu)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SanBong>()
                .HasOne(x => x.LoaiSan)
                .WithMany(x => x.SanBongs)
                .HasForeignKey(x => x.MaLoaiSan)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BangGia>()
                .HasOne(x => x.SanBong)
                .WithMany(x => x.BangGias)
                .HasForeignKey(x => x.MaSan)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PhieuDatSan>()
                .HasOne(x => x.KhachHang)
                .WithMany(x => x.PhieuDatSans)
                .HasForeignKey(x => x.MaKhachHang)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PhieuDatSan>()
                .HasOne(x => x.SanBong)
                .WithMany(x => x.PhieuDatSans)
                .HasForeignKey(x => x.MaSan)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PhieuDatSan>()
                .HasOne(x => x.HoaDon)
                .WithOne(x => x.PhieuDatSan)
                .HasForeignKey<HoaDon>(x => x.MaPhieu)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<HoaDon>()
                .HasOne(x => x.NhanVienLap)
                .WithMany(x => x.HoaDons)
                .HasForeignKey(x => x.MaNhanVienLap)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ChiTietHoaDon>()
                .HasOne(x => x.HoaDon)
                .WithMany(x => x.ChiTietHoaDons)
                .HasForeignKey(x => x.MaHoaDon)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ChiTietHoaDon>()
                .HasOne(x => x.DichVu)
                .WithMany(x => x.ChiTietHoaDons)
                .HasForeignKey(x => x.MaDichVu)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CaLamViec>()
                .HasOne(x => x.NhanVien)
                .WithMany(x => x.CaLamViecs)
                .HasForeignKey(x => x.MaNhanVien)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LichLam>()
                .HasOne(x => x.NhanVien)
                .WithMany(x => x.LichLams)
                .HasForeignKey(x => x.MaNhanVien)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LichLam>()
                .HasOne(x => x.CaLamViec)
                .WithMany(x => x.LichLams)
                .HasForeignKey(x => x.MaCa)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BangGia>().Property(x => x.GiaTien).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<PhieuDatSan>().Property(x => x.TongTienSan).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<HoaDon>().Property(x => x.TongTienSan).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<HoaDon>().Property(x => x.TongTienDichVu).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<HoaDon>().Property(x => x.TongCong).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<ChiTietHoaDon>().Property(x => x.DonGia).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<ChiTietHoaDon>().Property(x => x.ThanhTien).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<DichVu>().Property(x => x.DonGia).HasColumnType("decimal(18,2)");

            modelBuilder.Entity<PhieuDatSan>().Property(x => x.NgayDat).HasColumnType("date");
            modelBuilder.Entity<LichLam>().Property(x => x.NgayLam).HasColumnType("date");

            modelBuilder.Entity<PhieuDatSan>().Property(x => x.GioBatDau).HasColumnType("time");
            modelBuilder.Entity<PhieuDatSan>().Property(x => x.GioKetThuc).HasColumnType("time");
            modelBuilder.Entity<BangGia>().Property(x => x.GioBatDau).HasColumnType("time");
            modelBuilder.Entity<BangGia>().Property(x => x.GioKetThuc).HasColumnType("time");
            modelBuilder.Entity<CaLamViec>().Property(x => x.ThoiGianBatDau).HasColumnType("time");
            modelBuilder.Entity<CaLamViec>().Property(x => x.ThoiGianKetThuc).HasColumnType("time");
        }
    }