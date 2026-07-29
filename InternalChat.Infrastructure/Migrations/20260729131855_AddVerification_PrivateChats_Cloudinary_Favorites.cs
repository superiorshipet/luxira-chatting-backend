using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternalChat.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVerification_PrivateChats_Cloudinary_Favorites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CanReceivePrivateMessages",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsVerified",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PasswordResetToken",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordResetTokenExpiresAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPrivate",
                table: "Groups",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "PrivateTargetUserId",
                table: "Groups",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CapturedMedia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CloudinaryUrl = table.Column<string>(type: "text", nullable: false),
                    CloudinaryPublicId = table.Column<string>(type: "text", nullable: false),
                    CapturedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LinkedMessageId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CapturedMedia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CapturedMedia_Messages_LinkedMessageId",
                        column: x => x.LinkedMessageId,
                        principalTable: "Messages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CapturedMedia_Users_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserFavoriteGroups",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    FavoritedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserFavoriteGroups", x => new { x.UserId, x.GroupId });
                    table.ForeignKey(
                        name: "FK_UserFavoriteGroups_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserFavoriteGroups_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true,
                filter: "\"Email\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_PrivateTargetUserId",
                table: "Groups",
                column: "PrivateTargetUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CapturedMedia_LinkedMessageId",
                table: "CapturedMedia",
                column: "LinkedMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_CapturedMedia_UploadedByUserId",
                table: "CapturedMedia",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserFavoriteGroups_GroupId",
                table: "UserFavoriteGroups",
                column: "GroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_Groups_Users_PrivateTargetUserId",
                table: "Groups",
                column: "PrivateTargetUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Groups_Users_PrivateTargetUserId",
                table: "Groups");

            migrationBuilder.DropTable(
                name: "CapturedMedia");

            migrationBuilder.DropTable(
                name: "UserFavoriteGroups");

            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Groups_PrivateTargetUserId",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "CanReceivePrivateMessages",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsVerified",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PasswordResetToken",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PasswordResetTokenExpiresAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsPrivate",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "PrivateTargetUserId",
                table: "Groups");
        }
    }
}
