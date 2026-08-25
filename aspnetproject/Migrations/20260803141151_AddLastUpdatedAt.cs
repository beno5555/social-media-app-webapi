using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace aspnetproject.Migrations
{
    /// <inheritdoc />
    public partial class AddLastUpdatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdatedAt",
                table: "Users",
                type: "datetime2(3)",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdatedAt",
                table: "Posts",
                type: "datetime2(3)",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdatedAt",
                table: "Messages",
                type: "datetime2(3)",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdatedAt",
                table: "Friendships",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdatedAt",
                table: "Comments",
                type: "datetime2(3)",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastUpdatedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastUpdatedAt",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "LastUpdatedAt",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "LastUpdatedAt",
                table: "Friendships");

            migrationBuilder.DropColumn(
                name: "LastUpdatedAt",
                table: "Comments");
        }
    }
}
