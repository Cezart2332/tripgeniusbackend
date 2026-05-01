using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TripGeniusBackend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactoredInvites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invites_Users_UserId",
                table: "Invites");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Invites",
                newName: "InviterId");

            migrationBuilder.RenameIndex(
                name: "IX_Invites_UserId",
                table: "Invites",
                newName: "IX_Invites_InviterId");

            migrationBuilder.AddColumn<int>(
                name: "InvitedId",
                table: "Invites",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Invites_InvitedId",
                table: "Invites",
                column: "InvitedId");

            migrationBuilder.AddForeignKey(
                name: "FK_Invites_Users_InvitedId",
                table: "Invites",
                column: "InvitedId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Invites_Users_InviterId",
                table: "Invites",
                column: "InviterId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invites_Users_InvitedId",
                table: "Invites");

            migrationBuilder.DropForeignKey(
                name: "FK_Invites_Users_InviterId",
                table: "Invites");

            migrationBuilder.DropIndex(
                name: "IX_Invites_InvitedId",
                table: "Invites");

            migrationBuilder.DropColumn(
                name: "InvitedId",
                table: "Invites");

            migrationBuilder.RenameColumn(
                name: "InviterId",
                table: "Invites",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Invites_InviterId",
                table: "Invites",
                newName: "IX_Invites_UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Invites_Users_UserId",
                table: "Invites",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
