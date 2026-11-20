using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopElegance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeWhiteCoatVariantIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "WhiteCoatVariantId",
                table: "Customizations",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "WhiteCoatVariantId",
                table: "Customizations",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
