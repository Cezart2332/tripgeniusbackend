using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TripGeniusBackend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMessageSenderType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Users_UserId",
                schema: "public",
                table: "Messages");

            migrationBuilder.DropForeignKey(
                name: "FK_OffroadMessages_Users_UserId",
                schema: "public",
                table: "OffroadMessages");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                schema: "public",
                table: "OffroadMessages",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "SenderType",
                schema: "public",
                table: "OffroadMessages",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                schema: "public",
                table: "Messages",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "SenderType",
                schema: "public",
                table: "Messages",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Users_UserId",
                schema: "public",
                table: "Messages",
                column: "UserId",
                principalSchema: "public",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OffroadMessages_Users_UserId",
                schema: "public",
                table: "OffroadMessages",
                column: "UserId",
                principalSchema: "public",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Users_UserId",
                schema: "public",
                table: "Messages");

            migrationBuilder.DropForeignKey(
                name: "FK_OffroadMessages_Users_UserId",
                schema: "public",
                table: "OffroadMessages");

            migrationBuilder.DropColumn(
                name: "SenderType",
                schema: "public",
                table: "OffroadMessages");

            migrationBuilder.DropColumn(
                name: "SenderType",
                schema: "public",
                table: "Messages");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                schema: "public",
                table: "OffroadMessages",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                schema: "public",
                table: "Messages",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Users_UserId",
                schema: "public",
                table: "Messages",
                column: "UserId",
                principalSchema: "public",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OffroadMessages_Users_UserId",
                schema: "public",
                table: "OffroadMessages",
                column: "UserId",
                principalSchema: "public",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
