using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLySanBong.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueAccountFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_NguoiDungs_Email",
                table: "NguoiDungs",
                column: "Email",
                unique: true,
                filter: "[Email] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_NguoiDungs_SoDienThoai",
                table: "NguoiDungs",
                column: "SoDienThoai",
                unique: true,
                filter: "[SoDienThoai] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NguoiDungs_Email",
                table: "NguoiDungs");

            migrationBuilder.DropIndex(
                name: "IX_NguoiDungs_SoDienThoai",
                table: "NguoiDungs");
        }
    }
}
