using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutuMuch.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddShipOrder : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Carrier",
            table: "Orders",
            type: "nvarchar(8)",
            maxLength: 8,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "DeliveredAt",
            table: "Orders",
            type: "datetime2",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "EndedAt",
            table: "Orders",
            type: "datetime2",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "ShipBy",
            table: "Orders",
            type: "datetime2",
            nullable: false,
            defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

        // Orders paid before this migration: 7 calendar days always holds 5 weekdays.
        migrationBuilder.Sql("UPDATE Orders SET ShipBy = DATEADD(day, 7, PaidAt)");

        migrationBuilder.AddColumn<DateTime>(
            name: "ShippedAt",
            table: "Orders",
            type: "datetime2",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "TrackingNumber",
            table: "Orders",
            type: "nvarchar(34)",
            maxLength: 34,
            nullable: true);

        migrationBuilder.CreateTable(
            name: "OrderStatusChanges",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                From = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                To = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                Actor = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_OrderStatusChanges", x => x.Id);
                table.ForeignKey(
                    name: "FK_OrderStatusChanges_Orders_OrderId",
                    column: x => x.OrderId,
                    principalTable: "Orders",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Orders_Carrier_TrackingNumber",
            table: "Orders",
            columns: new[] { "Carrier", "TrackingNumber" });

        migrationBuilder.CreateIndex(
            name: "IX_Orders_Status_ShipBy",
            table: "Orders",
            columns: new[] { "Status", "ShipBy" });

        migrationBuilder.CreateIndex(
            name: "IX_OrderStatusChanges_OrderId_OccurredAt",
            table: "OrderStatusChanges",
            columns: new[] { "OrderId", "OccurredAt" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "OrderStatusChanges");

        migrationBuilder.DropIndex(
            name: "IX_Orders_Carrier_TrackingNumber",
            table: "Orders");

        migrationBuilder.DropIndex(
            name: "IX_Orders_Status_ShipBy",
            table: "Orders");

        migrationBuilder.DropColumn(
            name: "Carrier",
            table: "Orders");

        migrationBuilder.DropColumn(
            name: "DeliveredAt",
            table: "Orders");

        migrationBuilder.DropColumn(
            name: "EndedAt",
            table: "Orders");

        migrationBuilder.DropColumn(
            name: "ShipBy",
            table: "Orders");

        migrationBuilder.DropColumn(
            name: "ShippedAt",
            table: "Orders");

        migrationBuilder.DropColumn(
            name: "TrackingNumber",
            table: "Orders");
    }
}
