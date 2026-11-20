using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLySanBong.Migrations
{
    /// <inheritdoc />
    public partial class AddCourtLocationAndOpenReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DiaChi",
                table: "SanBongs",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "KinhDo",
                table: "SanBongs",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ViDo",
                table: "SanBongs",
                type: "float",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "MaPhieu",
                table: "DanhGias",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "MaSan",
                table: "DanhGias",
                type: "int",
                nullable: true);

            migrationBuilder.Sql("UPDATE [DanhGias] SET [MaSan] = [PhieuDatSans].[MaSan] FROM [DanhGias] INNER JOIN [PhieuDatSans] ON [DanhGias].[MaPhieu] = [PhieuDatSans].[MaPhieu]");

            migrationBuilder.AlterColumn<int>(
                name: "MaSan",
                table: "DanhGias",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DanhGias_MaSan",
                table: "DanhGias",
                column: "MaSan");

            migrationBuilder.AddForeignKey(
                name: "FK_DanhGias_SanBongs_MaSan",
                table: "DanhGias",
                column: "MaSan",
                principalTable: "SanBongs",
                principalColumn: "MaSan",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DanhGias_SanBongs_MaSan",
                table: "DanhGias");

            migrationBuilder.DropIndex(
                name: "IX_DanhGias_MaSan",
                table: "DanhGias");

            migrationBuilder.DropColumn(
                name: "DiaChi",
                table: "SanBongs");

            migrationBuilder.DropColumn(
                name: "KinhDo",
                table: "SanBongs");

            migrationBuilder.DropColumn(
                name: "ViDo",
                table: "SanBongs");

            migrationBuilder.DropColumn(
                name: "MaSan",
                table: "DanhGias");

            migrationBuilder.AlterColumn<int>(
                name: "MaPhieu",
                table: "DanhGias",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
