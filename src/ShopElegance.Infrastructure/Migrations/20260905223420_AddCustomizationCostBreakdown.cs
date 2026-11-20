using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopElegance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomizationCostBreakdown : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "EmbroideryCost",
                table: "Customizations",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "LogoCost",
                table: "Customizations",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmbroideryCost",
                table: "Customizations");

            migrationBuilder.DropColumn(
                name: "LogoCost",
                table: "Customizations");
        }
    }
}
