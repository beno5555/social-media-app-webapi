using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace aspnetproject.Migrations
{
    /// <inheritdoc />
    public partial class SeedDataMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Bio", "CreatedAt", "DateOfBirth", "Email", "LastOnlineAt", "LastUpdatedAt", "PasswordHash", "PasswordSalt", "ResetTokenExpiresAt", "ResetTokenHash", "Username", "UsernameLastChangedAt" },
                values: new object[,]
                {
                    { 2, "Primary test caller", new DateTime(2026, 9, 6, 10, 50, 0, 0, DateTimeKind.Utc), new DateTime(2000, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "alice@example.com", null, null, "x3UgoGJFZ2X3VJBAaQyF4J+M9+gfXP6vdO1W4jNwUzQ=", "o0wzhm224CIRy46p73NmiqjhaEtU5pyOuZ60rUHOM+g=", null, "", "alice", null },
                    { 3, "Alice's accepted friend", new DateTime(2026, 9, 6, 10, 55, 0, 0, DateTimeKind.Utc), new DateTime(2000, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "bob@example.com", null, null, "x3UgoGJFZ2X3VJBAaQyF4J+M9+gfXP6vdO1W4jNwUzQ=", "o0wzhm224CIRy46p73NmiqjhaEtU5pyOuZ60rUHOM+g=", null, "", "bob", null },
                    { 4, "Unrelated active user", new DateTime(2026, 9, 6, 11, 0, 0, 0, DateTimeKind.Utc), new DateTime(2000, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "charlie@example.com", null, null, "x3UgoGJFZ2X3VJBAaQyF4J+M9+gfXP6vdO1W4jNwUzQ=", "o0wzhm224CIRy46p73NmiqjhaEtU5pyOuZ60rUHOM+g=", null, "", "charlie", null },
                    { 5, "Pending friendship user", new DateTime(2026, 9, 6, 11, 5, 0, 0, DateTimeKind.Utc), new DateTime(2000, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "diana@example.com", null, null, "x3UgoGJFZ2X3VJBAaQyF4J+M9+gfXP6vdO1W4jNwUzQ=", "o0wzhm224CIRy46p73NmiqjhaEtU5pyOuZ60rUHOM+g=", null, "", "diana", null }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "AccountDeactivatedAt", "Bio", "CreatedAt", "DateOfBirth", "Email", "LastOnlineAt", "LastUpdatedAt", "PasswordHash", "PasswordSalt", "ResetTokenExpiresAt", "ResetTokenHash", "Username", "UsernameLastChangedAt" },
                values: new object[] { 6, new DateTime(2026, 9, 6, 11, 50, 0, 0, DateTimeKind.Utc), "Deactivated fixture user", new DateTime(2026, 9, 6, 11, 10, 0, 0, DateTimeKind.Utc), new DateTime(2000, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "erin@example.com", null, null, "x3UgoGJFZ2X3VJBAaQyF4J+M9+gfXP6vdO1W4jNwUzQ=", "o0wzhm224CIRy46p73NmiqjhaEtU5pyOuZ60rUHOM+g=", new DateTime(2026, 9, 7, 12, 0, 0, 0, DateTimeKind.Utc), "ERINactivationTokenHash000000000000000000", "erin_deactivated", null });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "AccountDeletedAt", "Bio", "CreatedAt", "DateOfBirth", "Email", "LastOnlineAt", "LastUpdatedAt", "PasswordHash", "PasswordSalt", "ResetTokenExpiresAt", "ResetTokenHash", "Username", "UsernameLastChangedAt" },
                values: new object[] { 7, new DateTime(2026, 8, 12, 12, 0, 0, 0, DateTimeKind.Utc), "Soft-deleted fixture user", new DateTime(2026, 9, 6, 11, 15, 0, 0, DateTimeKind.Utc), new DateTime(2000, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "frank@example.com", null, null, "x3UgoGJFZ2X3VJBAaQyF4J+M9+gfXP6vdO1W4jNwUzQ=", "o0wzhm224CIRy46p73NmiqjhaEtU5pyOuZ60rUHOM+g=", null, "", "frank_deleted", null });

            migrationBuilder.InsertData(
                table: "Friendships",
                columns: new[] { "Id", "AddresseeUserId", "CreatedAt", "FriendshipStatus", "LastUpdatedAt", "RequesterUserId", "SentAt" },
                values: new object[,]
                {
                    { 1, 3, new DateTime(2026, 9, 6, 11, 20, 0, 0, DateTimeKind.Utc), "Accepted", new DateTime(2026, 9, 6, 11, 21, 0, 0, DateTimeKind.Utc), 2, new DateTime(2026, 9, 6, 11, 20, 0, 0, DateTimeKind.Utc) },
                    { 2, 5, new DateTime(2026, 9, 6, 11, 22, 0, 0, DateTimeKind.Utc), "Pending", null, 2, new DateTime(2026, 9, 6, 11, 22, 0, 0, DateTimeKind.Utc) },
                    { 3, 2, new DateTime(2026, 9, 6, 11, 23, 0, 0, DateTimeKind.Utc), "Pending", null, 4, new DateTime(2026, 9, 6, 11, 23, 0, 0, DateTimeKind.Utc) },
                    { 4, 4, new DateTime(2026, 9, 6, 11, 24, 0, 0, DateTimeKind.Utc), "Declined", new DateTime(2026, 9, 6, 11, 25, 0, 0, DateTimeKind.Utc), 3, new DateTime(2026, 9, 6, 11, 24, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "Messages",
                columns: new[] { "Id", "CreatedAt", "LastUpdatedAt", "MessageContent", "ReceiverUserId", "Seen", "SeenAt", "SenderUserId" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 9, 6, 11, 36, 0, 0, DateTimeKind.Utc), null, "Old unread message from Bob to Alice.", 2, false, null, 3 },
                    { 2, new DateTime(2026, 9, 6, 11, 38, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 6, 11, 39, 0, 0, DateTimeKind.Utc), "Read message from Alice to Bob.", 3, true, new DateTime(2026, 9, 6, 11, 39, 0, 0, DateTimeKind.Utc), 2 },
                    { 3, new DateTime(2026, 9, 6, 11, 40, 0, 0, DateTimeKind.Utc), null, "Latest message from Alice to Bob.", 3, false, null, 2 },
                    { 4, new DateTime(2026, 9, 6, 11, 41, 0, 0, DateTimeKind.Utc), null, "Latest unread message from Bob to Alice.", 2, false, null, 3 }
                });

            migrationBuilder.InsertData(
                table: "Posts",
                columns: new[] { "Id", "CreatedAt", "LastUpdatedAt", "PostContent", "PostTitle", "UserId" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 9, 6, 11, 26, 0, 0, DateTimeKind.Utc), null, "Alice owns this post.", "Alice Post", 2 },
                    { 2, new DateTime(2026, 9, 6, 11, 27, 0, 0, DateTimeKind.Utc), null, "Bob's post should appear in Alice's feed.", "Bob Post", 3 },
                    { 3, new DateTime(2026, 9, 6, 11, 28, 0, 0, DateTimeKind.Utc), null, "Charlie's public post should not appear in Alice's friend feed.", "Charlie Post", 4 },
                    { 4, new DateTime(2026, 9, 6, 11, 29, 0, 0, DateTimeKind.Utc), null, "Post owned by a deactivated user.", "Erin Deactivated Post", 6 },
                    { 5, new DateTime(2026, 9, 6, 11, 30, 0, 0, DateTimeKind.Utc), null, "Post owned by a soft-deleted user.", "Frank Deleted Post", 7 }
                });

            migrationBuilder.InsertData(
                table: "RefreshTokens",
                columns: new[] { "Id", "CreatedAt", "ExpiresAt", "RevokedAt", "TokenHash", "UserId" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 9, 6, 11, 42, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 13, 12, 0, 0, 0, DateTimeKind.Utc), null, "ALICEactiveRefreshTokenHash000000000000000", 2 },
                    { 2, new DateTime(2026, 9, 6, 11, 43, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 13, 12, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 6, 11, 44, 0, 0, DateTimeKind.Utc), "ALICErevokedRefreshTokenHash00000000000000", 2 },
                    { 3, new DateTime(2026, 8, 29, 12, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 5, 12, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 4, 12, 0, 0, 0, DateTimeKind.Utc), "ALICEexpiredRefreshTokenHash00000000000000", 2 }
                });

            migrationBuilder.InsertData(
                table: "Comments",
                columns: new[] { "Id", "CommentContent", "CommenterUserId", "CreatedAt", "LastUpdatedAt", "PostId" },
                values: new object[,]
                {
                    { 1, "Alice comments on Bob's post.", 2, new DateTime(2026, 9, 6, 11, 31, 0, 0, DateTimeKind.Utc), null, 2 },
                    { 2, "Bob comments on his own post.", 3, new DateTime(2026, 9, 6, 11, 32, 0, 0, DateTimeKind.Utc), null, 2 },
                    { 3, "Charlie comments on Bob's post.", 4, new DateTime(2026, 9, 6, 11, 33, 0, 0, DateTimeKind.Utc), null, 2 },
                    { 4, "Bob comments on Alice's post.", 3, new DateTime(2026, 9, 6, 11, 34, 0, 0, DateTimeKind.Utc), null, 1 },
                    { 5, "Orphaned comment with no active author.", null, new DateTime(2026, 9, 6, 11, 35, 0, 0, DateTimeKind.Utc), null, 1 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Comments",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Comments",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Comments",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Comments",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Comments",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Friendships",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Friendships",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Friendships",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Friendships",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Messages",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Messages",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Messages",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Messages",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "RefreshTokens",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "RefreshTokens",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "RefreshTokens",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3);
        }
    }
}
