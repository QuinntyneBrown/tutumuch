using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TuTuMuch.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddAuctions : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "AuctionDays",
            table: "Listings",
            type: "int",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "AuctionEndsAt",
            table: "Listings",
            type: "datetime2",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "BidCount",
            table: "Listings",
            type: "int",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<decimal>(
            name: "CurrentBid",
            table: "Listings",
            type: "decimal(7,2)",
            precision: 7,
            scale: 2,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Format",
            table: "Listings",
            type: "nvarchar(16)",
            maxLength: 16,
            nullable: false,
            defaultValue: "FixedPrice");

        migrationBuilder.AddColumn<Guid>(
            name: "HighBidderId",
            table: "Listings",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "PayBy",
            table: "Listings",
            type: "datetime2",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "WinnerId",
            table: "Listings",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "Bids",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ListingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                BidderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Amount = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: false),
                PlacedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Bids", x => x.Id);
                table.CheckConstraint("CK_Bids_Amount", "CAST(Amount AS REAL) BETWEEN 1 AND 5000");
                table.ForeignKey(
                    name: "FK_Bids_Listings_ListingId",
                    column: x => x.ListingId,
                    principalTable: "Listings",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_Bids_Members_BidderId",
                    column: x => x.BidderId,
                    principalTable: "Members",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Listings_Format_Status_AuctionEndsAt",
            table: "Listings",
            columns: new[] { "Format", "Status", "AuctionEndsAt" });

        migrationBuilder.AddCheckConstraint(
            name: "CK_Listings_AuctionDays",
            table: "Listings",
            sql: "AuctionDays IS NULL OR AuctionDays IN (3, 5, 7)");

        migrationBuilder.AddCheckConstraint(
            name: "CK_Listings_CurrentBid",
            table: "Listings",
            sql: "CurrentBid IS NULL OR CAST(CurrentBid AS REAL) BETWEEN 1 AND 5000");

        migrationBuilder.CreateIndex(
            name: "IX_Bids_BidderId_ListingId",
            table: "Bids",
            columns: new[] { "BidderId", "ListingId" });

        migrationBuilder.CreateIndex(
            name: "IX_Bids_ListingId_PlacedAt",
            table: "Bids",
            columns: new[] { "ListingId", "PlacedAt" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Bids");

        migrationBuilder.DropIndex(
            name: "IX_Listings_Format_Status_AuctionEndsAt",
            table: "Listings");

        migrationBuilder.DropCheckConstraint(
            name: "CK_Listings_AuctionDays",
            table: "Listings");

        migrationBuilder.DropCheckConstraint(
            name: "CK_Listings_CurrentBid",
            table: "Listings");

        migrationBuilder.DropColumn(
            name: "AuctionDays",
            table: "Listings");

        migrationBuilder.DropColumn(
            name: "AuctionEndsAt",
            table: "Listings");

        migrationBuilder.DropColumn(
            name: "BidCount",
            table: "Listings");

        migrationBuilder.DropColumn(
            name: "CurrentBid",
            table: "Listings");

        migrationBuilder.DropColumn(
            name: "Format",
            table: "Listings");

        migrationBuilder.DropColumn(
            name: "HighBidderId",
            table: "Listings");

        migrationBuilder.DropColumn(
            name: "PayBy",
            table: "Listings");

        migrationBuilder.DropColumn(
            name: "WinnerId",
            table: "Listings");
    }
}
