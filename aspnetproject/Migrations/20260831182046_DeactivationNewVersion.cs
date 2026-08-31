using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace aspnetproject.Migrations
{
    /// <inheritdoc />
    public partial class DeactivationNewVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAccountDeactivated",
                table: "Users");

            migrationBuilder.AddColumn<DateTime>(
                name: "AccountDeactivatedAt",
                table: "Users",
                type: "datetime2",
                nullable: true,
                defaultValueSql: "NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountDeactivatedAt",
                table: "Users");

            migrationBuilder.AddColumn<bool>(
                name: "IsAccountDeactivated",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValueSql: "1");
        }
    }
}
