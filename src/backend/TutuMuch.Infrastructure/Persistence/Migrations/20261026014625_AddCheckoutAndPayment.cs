using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutuMuch.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddCheckoutAndPayment : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "ReservedByCheckoutId",
            table: "Listings",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "ReservedUntil",
            table: "Listings",
            type: "datetime2",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "Checkouts",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ListingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                BuyerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                ItemPrice = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                ShippingCost = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                SalesTax = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                Total = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                ShippingFullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                ShippingLine1 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                ShippingLine2 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                ShippingCity = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                ShippingState = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                ShippingZip = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                ShippingCountry = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                LastDeclineReason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                EndedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Checkouts", x => x.Id);
                table.ForeignKey(
                    name: "FK_Checkouts_Listings_ListingId",
                    column: x => x.ListingId,
                    principalTable: "Listings",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Checkouts_Members_BuyerId",
                    column: x => x.BuyerId,
                    principalTable: "Members",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "PaymentWebhookEvents",
            columns: table => new
            {
                ProviderEventId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Type = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PaymentWebhookEvents", x => x.ProviderEventId);
            });

        migrationBuilder.CreateTable(
            name: "SavedAddresses",
            columns: table => new
            {
                MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Line1 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Line2 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                City = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                State = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                Zip = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                Country = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SavedAddresses", x => x.MemberId);
                table.ForeignKey(
                    name: "FK_SavedAddresses_Members_MemberId",
                    column: x => x.MemberId,
                    principalTable: "Members",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Payments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CheckoutId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                IdempotencyKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Amount = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                ProviderPaymentId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                CardBrand = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                CardLast4 = table.Column<string>(type: "nchar(4)", fixedLength: true, maxLength: 4, nullable: true),
                DeclineReason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                SettledAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Payments", x => x.Id);
                table.ForeignKey(
                    name: "FK_Payments_Checkouts_CheckoutId",
                    column: x => x.CheckoutId,
                    principalTable: "Checkouts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Orders",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Number = table.Column<long>(type: "bigint", nullable: false),
                Reference = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                CheckoutId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ListingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                BuyerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SellerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                ItemPrice = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                ShippingCost = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                SalesTax = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                Total = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                ShippingFullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                ShippingLine1 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                ShippingLine2 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                ShippingCity = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                ShippingState = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                ShippingZip = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                ShippingCountry = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                PaidAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Orders", x => x.Id);
                table.ForeignKey(
                    name: "FK_Orders_Checkouts_CheckoutId",
                    column: x => x.CheckoutId,
                    principalTable: "Checkouts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Orders_Listings_ListingId",
                    column: x => x.ListingId,
                    principalTable: "Listings",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Orders_Members_BuyerId",
                    column: x => x.BuyerId,
                    principalTable: "Members",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Orders_Members_SellerId",
                    column: x => x.SellerId,
                    principalTable: "Members",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Orders_Payments_PaymentId",
                    column: x => x.PaymentId,
                    principalTable: "Payments",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Refunds",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Amount = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                Reason = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                ProviderRefundId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Refunds", x => x.Id);
                table.ForeignKey(
                    name: "FK_Refunds_Payments_PaymentId",
                    column: x => x.PaymentId,
                    principalTable: "Payments",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Listings_Status_ReservedUntil",
            table: "Listings",
            columns: new[] { "Status", "ReservedUntil" });

        migrationBuilder.CreateIndex(
            name: "IX_Checkouts_BuyerId",
            table: "Checkouts",
            column: "BuyerId");

        migrationBuilder.CreateIndex(
            name: "IX_Checkouts_Status_ExpiresAt",
            table: "Checkouts",
            columns: new[] { "Status", "ExpiresAt" });

        migrationBuilder.CreateIndex(
            name: "UX_Checkouts_ListingId_Open",
            table: "Checkouts",
            column: "ListingId",
            unique: true,
            filter: "Status IN ('Open', 'Processing')");

        migrationBuilder.CreateIndex(
            name: "IX_Orders_BuyerId_PaidAt",
            table: "Orders",
            columns: new[] { "BuyerId", "PaidAt" });

        migrationBuilder.CreateIndex(
            name: "IX_Orders_PaymentId",
            table: "Orders",
            column: "PaymentId");

        migrationBuilder.CreateIndex(
            name: "IX_Orders_SellerId_PaidAt",
            table: "Orders",
            columns: new[] { "SellerId", "PaidAt" });

        migrationBuilder.CreateIndex(
            name: "UX_Orders_CheckoutId",
            table: "Orders",
            column: "CheckoutId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "UX_Orders_ListingId",
            table: "Orders",
            column: "ListingId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "UX_Orders_Number",
            table: "Orders",
            column: "Number",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "UX_Orders_Reference",
            table: "Orders",
            column: "Reference",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Payments_CheckoutId",
            table: "Payments",
            column: "CheckoutId");

        migrationBuilder.CreateIndex(
            name: "UX_Payments_IdempotencyKey",
            table: "Payments",
            column: "IdempotencyKey",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Refunds_PaymentId",
            table: "Refunds",
            column: "PaymentId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Orders");

        migrationBuilder.DropTable(
            name: "PaymentWebhookEvents");

        migrationBuilder.DropTable(
            name: "Refunds");

        migrationBuilder.DropTable(
            name: "SavedAddresses");

        migrationBuilder.DropTable(
            name: "Payments");

        migrationBuilder.DropTable(
            name: "Checkouts");

        migrationBuilder.DropIndex(
            name: "IX_Listings_Status_ReservedUntil",
            table: "Listings");

        migrationBuilder.DropColumn(
            name: "ReservedByCheckoutId",
            table: "Listings");

        migrationBuilder.DropColumn(
            name: "ReservedUntil",
            table: "Listings");
    }
}
