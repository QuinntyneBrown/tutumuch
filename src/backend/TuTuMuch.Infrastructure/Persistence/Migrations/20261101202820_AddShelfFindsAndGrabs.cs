using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TuTuMuch.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddShelfFindsAndGrabs : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<Guid>(
            name: "CheckoutId",
            table: "Payments",
            type: "uniqueidentifier",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "uniqueidentifier");

        migrationBuilder.AddColumn<Guid>(
            name: "GrabId",
            table: "Payments",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "ShelfFinds",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Number = table.Column<long>(type: "bigint", nullable: false),
                Reference = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                PosterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Title = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                Category = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                ShelfPrice = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                StoreName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                StoreAddress = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                SeenOn = table.Column<DateOnly>(type: "date", nullable: false),
                Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                GrabFee = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ShelfFinds", x => x.Id);
                table.ForeignKey(
                    name: "FK_ShelfFinds_Members_PosterId",
                    column: x => x.PosterId,
                    principalTable: "Members",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Grabs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Number = table.Column<long>(type: "bigint", nullable: false),
                Reference = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                FindId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ShopperId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                ShelfPrice = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                GrabFee = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                SalesTax = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                Total = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                Deposit = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                ShippingFullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                ShippingLine1 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                ShippingLine2 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                ShippingCity = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                ShippingState = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                ShippingZip = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                ShippingCountry = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                PaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                PaymentDeclineReason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                UnavailableReason = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                RefundedAmount = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                AnswerBy = table.Column<DateTime>(type: "datetime2", nullable: true),
                AcceptedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                BuyBy = table.Column<DateTime>(type: "datetime2", nullable: true),
                BoughtAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                EndedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Grabs", x => x.Id);
                table.ForeignKey(
                    name: "FK_Grabs_Members_ShopperId",
                    column: x => x.ShopperId,
                    principalTable: "Members",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_Grabs_ShelfFinds_FindId",
                    column: x => x.FindId,
                    principalTable: "ShelfFinds",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Payments_GrabId",
            table: "Payments",
            column: "GrabId");

        migrationBuilder.CreateIndex(
            name: "IX_Grabs_ShopperId_CreatedAt",
            table: "Grabs",
            columns: new[] { "ShopperId", "CreatedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_Grabs_Status",
            table: "Grabs",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "UX_Grabs_FindId_Open",
            table: "Grabs",
            column: "FindId",
            unique: true,
            filter: "Status IN ('PaymentPending', 'Requested', 'Accepted', 'Bought')");

        migrationBuilder.CreateIndex(
            name: "UX_Grabs_Number",
            table: "Grabs",
            column: "Number",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "UX_Grabs_Reference",
            table: "Grabs",
            column: "Reference",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ShelfFinds_PosterId_CreatedAt",
            table: "ShelfFinds",
            columns: new[] { "PosterId", "CreatedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_ShelfFinds_Status_SeenOn_CreatedAt",
            table: "ShelfFinds",
            columns: new[] { "Status", "SeenOn", "CreatedAt" });

        migrationBuilder.CreateIndex(
            name: "UX_ShelfFinds_Number",
            table: "ShelfFinds",
            column: "Number",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "UX_ShelfFinds_Reference",
            table: "ShelfFinds",
            column: "Reference",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Grabs");

        migrationBuilder.DropTable(
            name: "ShelfFinds");

        migrationBuilder.DropIndex(
            name: "IX_Payments_GrabId",
            table: "Payments");

        migrationBuilder.DropColumn(
            name: "GrabId",
            table: "Payments");

        migrationBuilder.AlterColumn<Guid>(
            name: "CheckoutId",
            table: "Payments",
            type: "uniqueidentifier",
            nullable: false,
            defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
            oldClrType: typeof(Guid),
            oldType: "uniqueidentifier",
            oldNullable: true);
    }
}
