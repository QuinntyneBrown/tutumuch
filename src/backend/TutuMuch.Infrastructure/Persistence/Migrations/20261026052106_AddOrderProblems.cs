using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutuMuch.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddOrderProblems : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsAdministrator",
            table: "Members",
            type: "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.CreateTable(
            name: "OrderProblems",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Number = table.Column<long>(type: "bigint", nullable: false),
                Reference = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Reason = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                OpenedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                SellerResponse = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                SellerRespondedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                Outcome = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                RefundAmount = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                ResolutionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                ResolvedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_OrderProblems", x => x.Id);
                table.ForeignKey(
                    name: "FK_OrderProblems_Members_ResolvedBy",
                    column: x => x.ResolvedBy,
                    principalTable: "Members",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_OrderProblems_Orders_OrderId",
                    column: x => x.OrderId,
                    principalTable: "Orders",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "ProblemPhotos",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ProblemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Position = table.Column<int>(type: "int", nullable: false),
                StorageKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                ContentType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProblemPhotos", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProblemPhotos_OrderProblems_ProblemId",
                    column: x => x.ProblemId,
                    principalTable: "OrderProblems",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_OrderProblems_Open_OpenedAt",
            table: "OrderProblems",
            columns: new[] { "Status", "OpenedAt" },
            filter: "Status = 'Open'");

        migrationBuilder.CreateIndex(
            name: "IX_OrderProblems_ResolvedBy",
            table: "OrderProblems",
            column: "ResolvedBy");

        migrationBuilder.CreateIndex(
            name: "UX_OrderProblems_Number",
            table: "OrderProblems",
            column: "Number",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "UX_OrderProblems_OrderId",
            table: "OrderProblems",
            column: "OrderId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "UX_OrderProblems_Reference",
            table: "OrderProblems",
            column: "Reference",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ProblemPhotos_ProblemId",
            table: "ProblemPhotos",
            column: "ProblemId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ProblemPhotos");

        migrationBuilder.DropTable(
            name: "OrderProblems");

        migrationBuilder.DropColumn(
            name: "IsAdministrator",
            table: "Members");
    }
}
