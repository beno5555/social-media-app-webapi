using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace aspnetproject.Migrations
{
    /// <inheritdoc />
    public partial class AddModerationStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ModerationStatus",
                table: "Posts",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Visible");

            migrationBuilder.AddColumn<string>(
                name: "ModerationStatus",
                table: "Comments",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Visible");

            migrationBuilder.UpdateData(
                table: "Comments",
                keyColumn: "Id",
                keyValue: 1,
                column: "ModerationStatus",
                value: "Visible");

            migrationBuilder.UpdateData(
                table: "Comments",
                keyColumn: "Id",
                keyValue: 2,
                column: "ModerationStatus",
                value: "Visible");

            migrationBuilder.UpdateData(
                table: "Comments",
                keyColumn: "Id",
                keyValue: 3,
                column: "ModerationStatus",
                value: "Visible");

            migrationBuilder.UpdateData(
                table: "Comments",
                keyColumn: "Id",
                keyValue: 4,
                column: "ModerationStatus",
                value: "Visible");

            migrationBuilder.UpdateData(
                table: "Comments",
                keyColumn: "Id",
                keyValue: 5,
                column: "ModerationStatus",
                value: "Visible");

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 1,
                column: "ModerationStatus",
                value: "Visible");

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 2,
                column: "ModerationStatus",
                value: "Visible");

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 3,
                column: "ModerationStatus",
                value: "Visible");

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 4,
                column: "ModerationStatus",
                value: "Visible");

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 5,
                column: "ModerationStatus",
                value: "Visible");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ModerationStatus",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "ModerationStatus",
                table: "Comments");
        }
    }
}
