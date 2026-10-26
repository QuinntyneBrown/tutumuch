using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TuTuMuch.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddListingPhotos : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ListingPhotos",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ListingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                Position = table.Column<int>(type: "int", nullable: true),
                OriginalKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                OriginalContentType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                OriginalBytes = table.Column<long>(type: "bigint", nullable: false),
                OriginalHash = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                Width = table.Column<int>(type: "int", nullable: false),
                Height = table.Column<int>(type: "int", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ListingPhotos", x => x.Id);
                table.CheckConstraint("CK_ListingPhotos_Position", "Position IS NULL OR Position BETWEEN 0 AND 7");
                table.ForeignKey(
                    name: "FK_ListingPhotos_Listings_ListingId",
                    column: x => x.ListingId,
                    principalTable: "Listings",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_ListingPhotos_Members_OwnerId",
                    column: x => x.OwnerId,
                    principalTable: "Members",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "ListingPhotoVersions",
            columns: table => new
            {
                Size = table.Column<int>(type: "int", nullable: false),
                Format = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                PhotoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Width = table.Column<int>(type: "int", nullable: false),
                Height = table.Column<int>(type: "int", nullable: false),
                Key = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                Bytes = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ListingPhotoVersions", x => new { x.PhotoId, x.Size, x.Format });
                table.ForeignKey(
                    name: "FK_ListingPhotoVersions_ListingPhotos_PhotoId",
                    column: x => x.PhotoId,
                    principalTable: "ListingPhotos",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ListingPhotos_ListingId_Position",
            table: "ListingPhotos",
            columns: new[] { "ListingId", "Position" });

        migrationBuilder.CreateIndex(
            name: "IX_ListingPhotos_OwnerId_CreatedAt",
            table: "ListingPhotos",
            columns: new[] { "OwnerId", "CreatedAt" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ListingPhotoVersions");

        migrationBuilder.DropTable(
            name: "ListingPhotos");
    }
}
