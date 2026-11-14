using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace aspnetproject.Migrations
{
    /// <inheritdoc />
    public partial class AddPostLikes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PostLikes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PostId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostLikes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostLikes_Posts_PostId",
                        column: x => x.PostId,
                        principalTable: "Posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PostLikes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "PostLikes",
                columns: new[] { "Id", "CreatedAt", "LastUpdatedAt", "PostId", "UserId" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 9, 6, 11, 45, 0, 0, DateTimeKind.Utc), null, 2, 2 },
                    { 2, new DateTime(2026, 9, 6, 11, 46, 0, 0, DateTimeKind.Utc), null, 2, 4 },
                    { 3, new DateTime(2026, 9, 6, 11, 47, 0, 0, DateTimeKind.Utc), null, 2, 6 },
                    { 4, new DateTime(2026, 9, 6, 11, 48, 0, 0, DateTimeKind.Utc), null, 1, 3 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_PostLikes_PostId_UserId",
                table: "PostLikes",
                columns: new[] { "PostId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PostLikes_UserId",
                table: "PostLikes",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PostLikes");
        }
    }
}
