using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TuTuMuch.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Members",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                DisplayName = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                NormalizedDisplayName = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                NormalizedEmail = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                PasswordHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                EmailVerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Members", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "EmailVerificationTokens",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EmailVerificationTokens", x => x.Id);
                table.ForeignKey(
                    name: "FK_EmailVerificationTokens_Members_MemberId",
                    column: x => x.MemberId,
                    principalTable: "Members",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Listings",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Number = table.Column<long>(type: "bigint", nullable: false),
                Reference = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                SellerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                Category = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                Subcategory = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: true),
                Size = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                Title = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                Brand = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                Condition = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                Flaws = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                Price = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: true),
                ShippingCost = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                SizeConfirmedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Listings", x => x.Id);
                table.CheckConstraint("CK_Listings_Price", "Price IS NULL OR CAST(Price AS REAL) BETWEEN 1 AND 5000");
                table.CheckConstraint("CK_Listings_ShippingCost", "ShippingCost IS NULL OR CAST(ShippingCost AS REAL) BETWEEN 0 AND 100");
                table.CheckConstraint("CK_Listings_Size", "Size = 'Girls'''");
                table.ForeignKey(
                    name: "FK_Listings_Members_SellerId",
                    column: x => x.SellerId,
                    principalTable: "Members",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Sessions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                FamilyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                RotatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Sessions", x => x.Id);
                table.ForeignKey(
                    name: "FK_Sessions_Members_MemberId",
                    column: x => x.MemberId,
                    principalTable: "Members",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_EmailVerificationTokens_MemberId_CreatedAt",
            table: "EmailVerificationTokens",
            columns: new[] { "MemberId", "CreatedAt" });

        migrationBuilder.CreateIndex(
            name: "UX_EmailVerificationTokens_TokenHash",
            table: "EmailVerificationTokens",
            column: "TokenHash",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Listings_SellerId_CreatedAt",
            table: "Listings",
            columns: new[] { "SellerId", "CreatedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_Listings_SellerId_PublishedAt",
            table: "Listings",
            columns: new[] { "SellerId", "PublishedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_Listings_SellerId_Status_CreatedAt",
            table: "Listings",
            columns: new[] { "SellerId", "Status", "CreatedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_Listings_Status_PublishedAt",
            table: "Listings",
            columns: new[] { "Status", "PublishedAt" });

        migrationBuilder.CreateIndex(
            name: "UX_Listings_Number",
            table: "Listings",
            column: "Number",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "UX_Listings_Reference",
            table: "Listings",
            column: "Reference",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "UX_Members_NormalizedDisplayName",
            table: "Members",
            column: "NormalizedDisplayName",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "UX_Members_NormalizedEmail",
            table: "Members",
            column: "NormalizedEmail",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Sessions_FamilyId",
            table: "Sessions",
            column: "FamilyId");

        migrationBuilder.CreateIndex(
            name: "IX_Sessions_MemberId",
            table: "Sessions",
            column: "MemberId");

        migrationBuilder.CreateIndex(
            name: "UX_Sessions_TokenHash",
            table: "Sessions",
            column: "TokenHash",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "EmailVerificationTokens");

        migrationBuilder.DropTable(
            name: "Listings");

        migrationBuilder.DropTable(
            name: "Sessions");

        migrationBuilder.DropTable(
            name: "Members");
    }
}
