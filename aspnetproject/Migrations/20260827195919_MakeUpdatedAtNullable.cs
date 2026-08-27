using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace aspnetproject.Migrations
{
    /// <inheritdoc />
    public partial class MakeUpdatedAtNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdatedAt",
                table: "Users",
                type: "datetime2(3)",
                nullable: true,
                defaultValueSql: "GETUTCDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldDefaultValueSql: "GETUTCDATE()");

            migrationBuilder.Sql(@"
                UPDATE Users
                SET LastUpdatedAt = NULL
                WHERE LastUpdatedAt = CreatedAt OR LastUpdatedAt = '2026-08-03 14:12:23.873';
            ");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdatedAt",
                table: "RefreshTokens",
                type: "datetime2(3)",
                nullable: true,
                defaultValueSql: "GETUTCDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldDefaultValueSql: "GETUTCDATE()");

            migrationBuilder.Sql(@"
                UPDATE RefreshTokens
                SET LastUpdatedAt = NULL
                WHERE LastUpdatedAt = CreatedAt
                    OR LastUpdatedAt = '2026-08-25 12:10:54.175'
            ");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdatedAt",
                table: "Posts",
                type: "datetime2(3)",
                nullable: true,
                defaultValueSql: "GETUTCDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldDefaultValueSql: "GETUTCDATE()");

            migrationBuilder.Sql(@"
                UPDATE Posts
                SET LastUpdatedAt = NULL
                WHERE LastUpdatedAt = CreatedAt
                    OR LastUpdatedAt = '2026-08-03 14:12:23.920'
            ");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdatedAt",
                table: "Messages",
                type: "datetime2(3)",
                nullable: true,
                defaultValueSql: "GETUTCDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldDefaultValueSql: "GETUTCDATE()");

            migrationBuilder.Sql(@"
                UPDATE Messages
                SET LastUpdatedAt = NULL
                WHERE LastUpdatedAt = CreatedAt
                    OR LastUpdatedAt = '2026-08-03 14:12:23.930'
            ");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdatedAt",
                table: "Logs",
                type: "datetime2(3)",
                nullable: true,
                defaultValueSql: "GETUTCDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldDefaultValueSql: "GETUTCDATE()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdatedAt",
                table: "Friendships",
                type: "datetime2",
                nullable: true,
                defaultValueSql: "GETUTCDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETUTCDATE()");

            migrationBuilder.Sql(@"
                UPDATE Friendships
                SET LastUpdatedAt = NULL
                WHERE LastUpdatedAt = CreatedAt
                    OR LastUpdatedAt = '2026-08-03 14:12:23.9366667'
            ");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdatedAt",
                table: "Comments",
                type: "datetime2(3)",
                nullable: true,
                defaultValueSql: "GETUTCDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldDefaultValueSql: "GETUTCDATE()");
            
            migrationBuilder.Sql(@"
                UPDATE Comments
                SET LastUpdatedAt = NULL
                WHERE LastUpdatedAt = CreatedAt
                    OR LastUpdatedAt = '2026-08-03 14:12:23.943'
            ");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdatedAt",
                table: "Users",
                type: "datetime2(3)",
                nullable: false,
                defaultValueSql: "GETUTCDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldNullable: true,
                oldDefaultValueSql: "GETUTCDATE()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdatedAt",
                table: "RefreshTokens",
                type: "datetime2(3)",
                nullable: false,
                defaultValueSql: "GETUTCDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldNullable: true,
                oldDefaultValueSql: "GETUTCDATE()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdatedAt",
                table: "Posts",
                type: "datetime2(3)",
                nullable: false,
                defaultValueSql: "GETUTCDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldNullable: true,
                oldDefaultValueSql: "GETUTCDATE()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdatedAt",
                table: "Messages",
                type: "datetime2(3)",
                nullable: false,
                defaultValueSql: "GETUTCDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldNullable: true,
                oldDefaultValueSql: "GETUTCDATE()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdatedAt",
                table: "Logs",
                type: "datetime2(3)",
                nullable: false,
                defaultValueSql: "GETUTCDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldNullable: true,
                oldDefaultValueSql: "GETUTCDATE()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdatedAt",
                table: "Friendships",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true,
                oldDefaultValueSql: "GETUTCDATE()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdatedAt",
                table: "Comments",
                type: "datetime2(3)",
                nullable: false,
                defaultValueSql: "GETUTCDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldNullable: true,
                oldDefaultValueSql: "GETUTCDATE()");
        }
    }
}
