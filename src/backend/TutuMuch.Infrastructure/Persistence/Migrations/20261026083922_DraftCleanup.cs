using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutuMuch.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class DraftCleanup : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "DraftReminderSentAt",
            table: "Listings",
            type: "datetime2",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Listings_Draft_UpdatedAt",
            table: "Listings",
            column: "UpdatedAt",
            filter: "Status = 'Draft'");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Listings_Draft_UpdatedAt",
            table: "Listings");

        migrationBuilder.DropColumn(
            name: "DraftReminderSentAt",
            table: "Listings");
    }
}
