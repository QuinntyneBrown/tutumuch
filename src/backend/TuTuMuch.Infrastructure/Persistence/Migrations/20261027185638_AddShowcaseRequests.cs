using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TuTuMuch.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddShowcaseRequests : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsShowcase",
            table: "Listings",
            type: "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.CreateTable(
            name: "ShowcaseRequests",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ListingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ShowcaseRequests", x => x.Id);
                table.ForeignKey(
                    name: "FK_ShowcaseRequests_Listings_ListingId",
                    column: x => x.ListingId,
                    principalTable: "Listings",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ShowcaseRequests_Members_MemberId",
                    column: x => x.MemberId,
                    principalTable: "Members",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ShowcaseRequests_ListingId",
            table: "ShowcaseRequests",
            column: "ListingId");

        migrationBuilder.CreateIndex(
            name: "IX_ShowcaseRequests_MemberId_CreatedAt",
            table: "ShowcaseRequests",
            columns: new[] { "MemberId", "CreatedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_ShowcaseRequests_MemberId_ListingId",
            table: "ShowcaseRequests",
            columns: new[] { "MemberId", "ListingId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ShowcaseRequests_UpdatedAt",
            table: "ShowcaseRequests",
            column: "UpdatedAt");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ShowcaseRequests");

        migrationBuilder.DropColumn(
            name: "IsShowcase",
            table: "Listings");
    }
}
