using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TuTuMuch.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddSellerPayouts : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "FeeAmount",
            table: "Orders",
            type: "decimal(10,2)",
            precision: 10,
            scale: 2,
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<decimal>(
            name: "FeeRate",
            table: "Orders",
            type: "decimal(5,4)",
            precision: 5,
            scale: 4,
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.CreateTable(
            name: "Payouts",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SellerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                GrossAmount = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                FeeRate = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                FeeAmount = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                NetAmount = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                HoldReason = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: true),
                ProviderPayoutId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                ReleasableAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                FailedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                FailureReason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Payouts", x => x.Id);
                table.ForeignKey(
                    name: "FK_Payouts_Members_SellerId",
                    column: x => x.SellerId,
                    principalTable: "Members",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Payouts_Orders_OrderId",
                    column: x => x.OrderId,
                    principalTable: "Orders",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "SellerPayoutAccounts",
            columns: table => new
            {
                MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ProviderAccountId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                BankName = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                BankLast4 = table.Column<string>(type: "nchar(4)", fixedLength: true, maxLength: 4, nullable: true),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SellerPayoutAccounts", x => x.MemberId);
                table.ForeignKey(
                    name: "FK_SellerPayoutAccounts_Members_MemberId",
                    column: x => x.MemberId,
                    principalTable: "Members",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Payouts_SellerId_Status",
            table: "Payouts",
            columns: new[] { "SellerId", "Status" });

        migrationBuilder.CreateIndex(
            name: "IX_Payouts_Status",
            table: "Payouts",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "UX_Payouts_OrderId",
            table: "Payouts",
            column: "OrderId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "UX_SellerPayoutAccounts_ProviderAccountId",
            table: "SellerPayoutAccounts",
            column: "ProviderAccountId",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Payouts");

        migrationBuilder.DropTable(
            name: "SellerPayoutAccounts");

        migrationBuilder.DropColumn(
            name: "FeeAmount",
            table: "Orders");

        migrationBuilder.DropColumn(
            name: "FeeRate",
            table: "Orders");
    }
}
