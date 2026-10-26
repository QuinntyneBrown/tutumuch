using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TuTuMuch.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddOrderCancellation : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "UX_Orders_ListingId",
            table: "Orders");

        migrationBuilder.AddColumn<string>(
            name: "CancellationNote",
            table: "Orders",
            type: "nvarchar(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "CancellationReason",
            table: "Orders",
            type: "nvarchar(24)",
            maxLength: 24,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "CancellationRequestMessage",
            table: "Orders",
            type: "nvarchar(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "CancellationRequestStatus",
            table: "Orders",
            type: "nvarchar(16)",
            maxLength: 16,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "CancellationRequestedAt",
            table: "Orders",
            type: "datetime2",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "CancellationRespondedAt",
            table: "Orders",
            type: "datetime2",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "RemovalReason",
            table: "Listings",
            type: "nvarchar(24)",
            maxLength: 24,
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "UX_Orders_ListingId",
            table: "Orders",
            column: "ListingId",
            unique: true,
            filter: "Status <> 'Cancelled'");

        // Orders the ship deadline job cancelled before this migration: record the reason and take
        // their listings off sale so the seller can relist them, as every cancellation now does.
        migrationBuilder.Sql("UPDATE Orders SET CancellationReason = 'ShipDeadlinePassed' WHERE Status = 'Cancelled'");
        migrationBuilder.Sql("UPDATE Listings SET Status = 'Removed', RemovalReason = 'OrderCancelled' WHERE Status = 'Sold' AND Id IN (SELECT ListingId FROM Orders WHERE Status = 'Cancelled') AND Id NOT IN (SELECT ListingId FROM Orders WHERE Status <> 'Cancelled')");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "UX_Orders_ListingId",
            table: "Orders");

        migrationBuilder.DropColumn(
            name: "CancellationNote",
            table: "Orders");

        migrationBuilder.DropColumn(
            name: "CancellationReason",
            table: "Orders");

        migrationBuilder.DropColumn(
            name: "CancellationRequestMessage",
            table: "Orders");

        migrationBuilder.DropColumn(
            name: "CancellationRequestStatus",
            table: "Orders");

        migrationBuilder.DropColumn(
            name: "CancellationRequestedAt",
            table: "Orders");

        migrationBuilder.DropColumn(
            name: "CancellationRespondedAt",
            table: "Orders");

        migrationBuilder.DropColumn(
            name: "RemovalReason",
            table: "Listings");

        migrationBuilder.CreateIndex(
            name: "UX_Orders_ListingId",
            table: "Orders",
            column: "ListingId",
            unique: true);
    }
}
