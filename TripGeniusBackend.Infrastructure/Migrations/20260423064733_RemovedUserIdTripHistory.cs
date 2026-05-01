using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TripGeniusBackend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemovedUserIdTripHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TripHistories_Users_UserId",
                table: "TripHistories");

            migrationBuilder.DropIndex(
                name: "IX_TripHistories_UserId",
                table: "TripHistories");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "TripHistories");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "TripHistories",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_TripHistories_UserId",
                table: "TripHistories",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_TripHistories_Users_UserId",
                table: "TripHistories",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
