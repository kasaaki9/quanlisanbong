using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLySanBong.Migrations
{
    /// <inheritdoc />
    public partial class AddLockReasons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LyDoKhoa",
                table: "NhanViens",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LyDoKhoa",
                table: "NguoiDungs",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LyDoKhoa",
                table: "KhachHangs",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LyDoKhoa",
                table: "NhanViens");

            migrationBuilder.DropColumn(
                name: "LyDoKhoa",
                table: "NguoiDungs");

            migrationBuilder.DropColumn(
                name: "LyDoKhoa",
                table: "KhachHangs");
        }
    }
}
