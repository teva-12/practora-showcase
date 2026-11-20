using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopElegance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomizationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CoatType",
                table: "Customizations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "Normal");

            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "Customizations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "White");

            migrationBuilder.AddColumn<string>(
                name: "Fabric",
                table: "Customizations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "Cotton");

            migrationBuilder.AddColumn<string>(
                name: "Size",
                table: "Customizations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "Medium");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoatType",
                table: "Customizations");

            migrationBuilder.DropColumn(
                name: "Color",
                table: "Customizations");

            migrationBuilder.DropColumn(
                name: "Fabric",
                table: "Customizations");

            migrationBuilder.DropColumn(
                name: "Size",
                table: "Customizations");
        }
    }
}
