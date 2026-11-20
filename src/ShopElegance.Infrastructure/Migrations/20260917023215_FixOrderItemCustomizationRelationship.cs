using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopElegance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixOrderItemCustomizationRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrderItems_CustomizationId",
                table: "OrderItems");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_CustomizationId",
                table: "OrderItems",
                column: "CustomizationId",
                unique: true,
                filter: "[CustomizationId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrderItems_CustomizationId",
                table: "OrderItems");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_CustomizationId",
                table: "OrderItems",
                column: "CustomizationId");
        }
    }
}
