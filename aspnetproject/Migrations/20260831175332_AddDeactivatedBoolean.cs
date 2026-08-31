using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace aspnetproject.Migrations
{
    /// <inheritdoc />
    public partial class AddDeactivatedBoolean : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAccountDeactivated",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValueSql: "1");

            migrationBuilder.RenameColumn(
                name: "LastActiveAt",
                table: "Users",
                newName: "LastOnlineAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAccountDeactivated",
                table: "Users");
            
            migrationBuilder.RenameColumn(
                name: "LastOnlineAt",
                table: "Users",
                newName: "LastActiveAt");
        }
    }
}
