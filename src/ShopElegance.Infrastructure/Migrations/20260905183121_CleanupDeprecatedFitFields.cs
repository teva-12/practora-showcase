using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopElegance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CleanupDeprecatedFitFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WhiteCoatVariants_ProductId_Size_Color_Fabric_Sleeve_Gender_Fit",
                table: "WhiteCoatVariants");

            migrationBuilder.DropColumn(
                name: "CustomSizeDetails",
                table: "WhiteCoatVariants");

            migrationBuilder.DropColumn(
                name: "Fit",
                table: "WhiteCoatVariants");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "WhiteCoatVariants");

            migrationBuilder.DropColumn(
                name: "Sleeve",
                table: "WhiteCoatVariants");

            migrationBuilder.AlterColumn<string>(
                name: "Size",
                table: "WhiteCoatVariants",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "CoatType",
                table: "WhiteCoatVariants",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Normal");

            migrationBuilder.CreateIndex(
                name: "IX_WhiteCoatVariants_ProductId_Size_Color_Fabric_CoatType",
                table: "WhiteCoatVariants",
                columns: new[] { "ProductId", "Size", "Color", "Fabric", "CoatType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WhiteCoatVariants_ProductId_Size_Color_Fabric_CoatType",
                table: "WhiteCoatVariants");

            migrationBuilder.DropColumn(
                name: "CoatType",
                table: "WhiteCoatVariants");

            migrationBuilder.AlterColumn<int>(
                name: "Size",
                table: "WhiteCoatVariants",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<string>(
                name: "CustomSizeDetails",
                table: "WhiteCoatVariants",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Fit",
                table: "WhiteCoatVariants",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Gender",
                table: "WhiteCoatVariants",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Sleeve",
                table: "WhiteCoatVariants",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_WhiteCoatVariants_ProductId_Size_Color_Fabric_Sleeve_Gender_Fit",
                table: "WhiteCoatVariants",
                columns: new[] { "ProductId", "Size", "Color", "Fabric", "Sleeve", "Gender", "Fit" });
        }
    }
}
