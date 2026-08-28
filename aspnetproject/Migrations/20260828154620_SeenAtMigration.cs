using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace aspnetproject.Migrations
{
    /// <inheritdoc />
    public partial class SeenAtMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsRead",
                table: "Messages",
                newName: "Seen"
            );
            
            migrationBuilder.AddColumn<DateTime>(
                name: "SeenAt",
                table: "Messages",
                type: "datetime2(3)",
                nullable: true,
                defaultValueSql: "null");

            migrationBuilder.Sql("UPDATE Messages SET SeenAt = GETUTCDATE() WHERE Seen = 1 AND SeenAt is NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Seen",
                table: "Messages",
                newName: "IsRead"
            );
        }
    }
}
