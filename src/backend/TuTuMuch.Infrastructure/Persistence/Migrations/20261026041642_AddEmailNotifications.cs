using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TuTuMuch.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddEmailNotifications : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "ShipReminderDue",
            table: "Orders",
            type: "datetime2",
            nullable: false,
            defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

        migrationBuilder.AddColumn<DateTime>(
            name: "ShipReminderSentAt",
            table: "Orders",
            type: "datetime2",
            nullable: true);

        // Orders paid before this migration are past their reminder; mark them reminded so none is sent late.
        migrationBuilder.Sql("UPDATE Orders SET ShipReminderDue = ShipBy, ShipReminderSentAt = PaidAt");

        migrationBuilder.CreateTable(
            name: "OutboxMessages",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                NotificationType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                RecipientMemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                AttemptCount = table.Column<int>(type: "int", nullable: false),
                NextAttemptAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                LockedUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                LastError = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                SuppressionReason = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                ClaimToken = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_OutboxMessages", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Orders_Status_ShipReminderDue",
            table: "Orders",
            columns: new[] { "Status", "ShipReminderDue" });

        migrationBuilder.CreateIndex(
            name: "IX_OutboxMessages_Due",
            table: "OutboxMessages",
            columns: new[] { "Status", "NextAttemptAt" },
            filter: "Status IN ('Pending', 'Processing')");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "OutboxMessages");

        migrationBuilder.DropIndex(
            name: "IX_Orders_Status_ShipReminderDue",
            table: "Orders");

        migrationBuilder.DropColumn(
            name: "ShipReminderDue",
            table: "Orders");

        migrationBuilder.DropColumn(
            name: "ShipReminderSentAt",
            table: "Orders");
    }
}
