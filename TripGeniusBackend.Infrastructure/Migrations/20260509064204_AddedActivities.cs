using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TripGeniusBackend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddedActivities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Day",
                table: "TripTimelines",
                newName: "StartDay");

            migrationBuilder.AddColumn<int>(
                name: "EndDay",
                table: "TripTimelines",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "TripActivity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Link = table.Column<string>(type: "text", nullable: true),
                    Cost = table.Column<double>(type: "double precision", nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    TripTimelineId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripActivity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TripActivity_TripTimelines_TripTimelineId",
                        column: x => x.TripTimelineId,
                        principalTable: "TripTimelines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TripActivity_TripTimelineId",
                table: "TripActivity",
                column: "TripTimelineId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TripActivity");

            migrationBuilder.DropColumn(
                name: "EndDay",
                table: "TripTimelines");

            migrationBuilder.RenameColumn(
                name: "StartDay",
                table: "TripTimelines",
                newName: "Day");
        }
    }
}
