using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutuMuch.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddCompleteOrder : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "CompletedAt",
            table: "Orders",
            type: "datetime2",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Orders_Status_DeliveredAt",
            table: "Orders",
            columns: new[] { "Status", "DeliveredAt" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Orders_Status_DeliveredAt",
            table: "Orders");

        migrationBuilder.DropColumn(
            name: "CompletedAt",
            table: "Orders");
    }
}
