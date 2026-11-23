using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Smartwarranty_2.Migrations
{
    /// <inheritdoc />
    public partial class WarrantyClaimAndExpiryNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "WarrantyClaimed",
                table: "ServiceRequests",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WarrantyId",
                table: "Notifications",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_WarrantyId",
                table: "Notifications",
                column: "WarrantyId",
                unique: true,
                filter: "[WarrantyId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Warranties_WarrantyId",
                table: "Notifications",
                column: "WarrantyId",
                principalTable: "Warranties",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Warranties_WarrantyId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_WarrantyId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "WarrantyClaimed",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "WarrantyId",
                table: "Notifications");
        }
    }
}
