using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TuTuMuch.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddItemRequests : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "ItemRequestEmails",
            table: "NotificationPreferences",
            type: "bit",
            nullable: false,
            defaultValue: true);

        migrationBuilder.CreateTable(
            name: "ItemRequests",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Category = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                Size = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                Keywords = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                MaxPrice = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: true),
                MinimumCondition = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ItemRequests", x => x.Id);
                table.CheckConstraint("CK_ItemRequests_MaxPrice", "MaxPrice IS NULL OR CAST(MaxPrice AS REAL) BETWEEN 1 AND 5000");
                table.CheckConstraint("CK_ItemRequests_Size", "Size = 'Girls'''");
                table.ForeignKey(
                    name: "FK_ItemRequests_Members_MemberId",
                    column: x => x.MemberId,
                    principalTable: "Members",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ItemRequestMatches",
            columns: table => new
            {
                ItemRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ListingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                MatchedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ItemRequestMatches", x => new { x.ItemRequestId, x.ListingId });
                table.ForeignKey(
                    name: "FK_ItemRequestMatches_ItemRequests_ItemRequestId",
                    column: x => x.ItemRequestId,
                    principalTable: "ItemRequests",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ItemRequestMatches_Listings_ListingId",
                    column: x => x.ListingId,
                    principalTable: "Listings",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ItemRequestMatches_ListingId",
            table: "ItemRequestMatches",
            column: "ListingId");

        migrationBuilder.CreateIndex(
            name: "IX_ItemRequests_Category_ExpiresAt",
            table: "ItemRequests",
            columns: new[] { "Category", "ExpiresAt" });

        migrationBuilder.CreateIndex(
            name: "IX_ItemRequests_ExpiresAt",
            table: "ItemRequests",
            column: "ExpiresAt");

        migrationBuilder.CreateIndex(
            name: "IX_ItemRequests_MemberId_CreatedAt",
            table: "ItemRequests",
            columns: new[] { "MemberId", "CreatedAt" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ItemRequestMatches");

        migrationBuilder.DropTable(
            name: "ItemRequests");

        migrationBuilder.DropColumn(
            name: "ItemRequestEmails",
            table: "NotificationPreferences");
    }
}
