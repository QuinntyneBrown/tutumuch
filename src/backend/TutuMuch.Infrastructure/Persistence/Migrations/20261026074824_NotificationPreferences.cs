using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutuMuch.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class NotificationPreferences : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "NotificationPreferences",
            columns: table => new
            {
                MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ReviewEmails = table.Column<bool>(type: "bit", nullable: false),
                MarketingEmails = table.Column<bool>(type: "bit", nullable: false),
                MarketingOptedInAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                MarketingOptedOutAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                LastMarketingChangeSource = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                Version = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_NotificationPreferences", x => x.MemberId);
                table.ForeignKey(
                    name: "FK_NotificationPreferences_Members_MemberId",
                    column: x => x.MemberId,
                    principalTable: "Members",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "NotificationPreferences");
    }
}
