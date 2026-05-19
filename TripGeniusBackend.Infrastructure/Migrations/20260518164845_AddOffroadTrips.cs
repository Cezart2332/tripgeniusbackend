using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Pgvector;

#nullable disable

namespace TripGeniusBackend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOffroadTrips : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.RenameTable(
                name: "Users",
                newName: "Users",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "TripTimelines",
                newName: "TripTimelines",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "Trips",
                newName: "Trips",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "TripMembers",
                newName: "TripMembers",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "TripHistories",
                newName: "TripHistories",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "TripActivity",
                newName: "TripActivity",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "RefreshTokens",
                newName: "RefreshTokens",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "PushSubscriptions",
                newName: "PushSubscriptions",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "Profiles",
                newName: "Profiles",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "Preferences",
                newName: "Preferences",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "Notifications",
                newName: "Notifications",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "Messages",
                newName: "Messages",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "Bugs",
                newName: "Bugs",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "AiMemories",
                newName: "AiMemories",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "AiChatHistories",
                newName: "AiChatHistories",
                newSchema: "public");

            migrationBuilder.CreateTable(
                name: "OffroadTrips",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ImageUrl = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Tags = table.Column<List<string>>(type: "text[]", nullable: false),
                    MaxParticipants = table.Column<int>(type: "integer", nullable: false),
                    Price = table.Column<double>(type: "double precision", nullable: false),
                    Embedding = table.Column<Vector>(type: "vector(2048)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OffroadTrips", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OffroadMessages",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Content = table.Column<string>(type: "text", nullable: false),
                    ImageURL = table.Column<string>(type: "text", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    OffroadTripId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OffroadMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OffroadMessages_OffroadTrips_OffroadTripId",
                        column: x => x.OffroadTripId,
                        principalSchema: "public",
                        principalTable: "OffroadTrips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OffroadMessages_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OffroadRoutes",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OffroadTripId = table.Column<int>(type: "integer", nullable: false),
                    StartDay = table.Column<int>(type: "integer", nullable: false),
                    EndDay = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: false),
                    TrackGeoJson = table.Column<string>(type: "jsonb", nullable: false),
                    OriginalGpx = table.Column<string>(type: "text", nullable: true),
                    Source = table.Column<string>(type: "text", nullable: false),
                    DistanceMeters = table.Column<double>(type: "double precision", nullable: false),
                    ElevationGainMeters = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OffroadRoutes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OffroadRoutes_OffroadTrips_OffroadTripId",
                        column: x => x.OffroadTripId,
                        principalSchema: "public",
                        principalTable: "OffroadTrips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OffroadTripHistories",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OffroadTripId = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OffroadTripHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OffroadTripHistories_OffroadTrips_OffroadTripId",
                        column: x => x.OffroadTripId,
                        principalSchema: "public",
                        principalTable: "OffroadTrips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OffroadTripMembers",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Type = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    OffroadTripId = table.Column<int>(type: "integer", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    MemberStatus = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OffroadTripMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OffroadTripMembers_OffroadTrips_OffroadTripId",
                        column: x => x.OffroadTripId,
                        principalSchema: "public",
                        principalTable: "OffroadTrips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OffroadTripMembers_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OffroadMessages_OffroadTripId",
                schema: "public",
                table: "OffroadMessages",
                column: "OffroadTripId");

            migrationBuilder.CreateIndex(
                name: "IX_OffroadMessages_UserId",
                schema: "public",
                table: "OffroadMessages",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_OffroadRoutes_OffroadTripId",
                schema: "public",
                table: "OffroadRoutes",
                column: "OffroadTripId");

            migrationBuilder.CreateIndex(
                name: "IX_OffroadTripHistories_OffroadTripId",
                schema: "public",
                table: "OffroadTripHistories",
                column: "OffroadTripId");

            migrationBuilder.CreateIndex(
                name: "IX_OffroadTripMembers_OffroadTripId",
                schema: "public",
                table: "OffroadTripMembers",
                column: "OffroadTripId");

            migrationBuilder.CreateIndex(
                name: "IX_OffroadTripMembers_UserId",
                schema: "public",
                table: "OffroadTripMembers",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OffroadMessages",
                schema: "public");

            migrationBuilder.DropTable(
                name: "OffroadRoutes",
                schema: "public");

            migrationBuilder.DropTable(
                name: "OffroadTripHistories",
                schema: "public");

            migrationBuilder.DropTable(
                name: "OffroadTripMembers",
                schema: "public");

            migrationBuilder.DropTable(
                name: "OffroadTrips",
                schema: "public");

            migrationBuilder.RenameTable(
                name: "Users",
                schema: "public",
                newName: "Users");

            migrationBuilder.RenameTable(
                name: "TripTimelines",
                schema: "public",
                newName: "TripTimelines");

            migrationBuilder.RenameTable(
                name: "Trips",
                schema: "public",
                newName: "Trips");

            migrationBuilder.RenameTable(
                name: "TripMembers",
                schema: "public",
                newName: "TripMembers");

            migrationBuilder.RenameTable(
                name: "TripHistories",
                schema: "public",
                newName: "TripHistories");

            migrationBuilder.RenameTable(
                name: "TripActivity",
                schema: "public",
                newName: "TripActivity");

            migrationBuilder.RenameTable(
                name: "RefreshTokens",
                schema: "public",
                newName: "RefreshTokens");

            migrationBuilder.RenameTable(
                name: "PushSubscriptions",
                schema: "public",
                newName: "PushSubscriptions");

            migrationBuilder.RenameTable(
                name: "Profiles",
                schema: "public",
                newName: "Profiles");

            migrationBuilder.RenameTable(
                name: "Preferences",
                schema: "public",
                newName: "Preferences");

            migrationBuilder.RenameTable(
                name: "Notifications",
                schema: "public",
                newName: "Notifications");

            migrationBuilder.RenameTable(
                name: "Messages",
                schema: "public",
                newName: "Messages");

            migrationBuilder.RenameTable(
                name: "Bugs",
                schema: "public",
                newName: "Bugs");

            migrationBuilder.RenameTable(
                name: "AiMemories",
                schema: "public",
                newName: "AiMemories");

            migrationBuilder.RenameTable(
                name: "AiChatHistories",
                schema: "public",
                newName: "AiChatHistories");
        }
    }
}
