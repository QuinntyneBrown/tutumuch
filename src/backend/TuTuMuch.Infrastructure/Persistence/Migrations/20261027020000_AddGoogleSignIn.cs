using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TuTuMuch.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddGoogleSignIn : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "PasswordHash",
            table: "Members",
            type: "nvarchar(256)",
            maxLength: 256,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "nvarchar(256)",
            oldMaxLength: 256);

        migrationBuilder.CreateTable(
            name: "ExternalLogins",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Provider = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                Subject = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ExternalLogins", x => x.Id);
                table.ForeignKey(
                    name: "FK_ExternalLogins_Members_MemberId",
                    column: x => x.MemberId,
                    principalTable: "Members",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "PendingExternalJoins",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Provider = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                Subject = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                TokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UsedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PendingExternalJoins", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ExternalLogins_MemberId",
            table: "ExternalLogins",
            column: "MemberId");

        migrationBuilder.CreateIndex(
            name: "UX_ExternalLogins_Provider_Subject",
            table: "ExternalLogins",
            columns: new[] { "Provider", "Subject" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "UX_PendingExternalJoins_TokenHash",
            table: "PendingExternalJoins",
            column: "TokenHash",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ExternalLogins");

        migrationBuilder.DropTable(
            name: "PendingExternalJoins");

        migrationBuilder.AlterColumn<string>(
            name: "PasswordHash",
            table: "Members",
            type: "nvarchar(256)",
            maxLength: 256,
            nullable: false,
            defaultValue: "",
            oldClrType: typeof(string),
            oldType: "nvarchar(256)",
            oldMaxLength: 256,
            oldNullable: true);
    }
}
