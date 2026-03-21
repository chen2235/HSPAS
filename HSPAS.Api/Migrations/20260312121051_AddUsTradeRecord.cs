using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HSPAS.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUsTradeRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "US_TradeRecord",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TradeDate = table.Column<DateTime>(type: "date", nullable: false),
                    SettlementDate = table.Column<DateTime>(type: "date", nullable: true),
                    StockSymbol = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StockName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Market = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(19,6)", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(19,6)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(19,4)", nullable: false),
                    Fee = table.Column<decimal>(type: "decimal(19,4)", nullable: false),
                    Tax = table.Column<decimal>(type: "decimal(19,4)", nullable: false),
                    NetAmount = table.Column<decimal>(type: "decimal(19,4)", nullable: false),
                    SettlementCurrency = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    ExchangeRate = table.Column<decimal>(type: "decimal(19,4)", nullable: true),
                    NetAmountTwd = table.Column<decimal>(type: "decimal(19,4)", nullable: true),
                    TradeRef = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreateTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_US_TradeRecord", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_US_TradeRecord_StockSymbol",
                table: "US_TradeRecord",
                column: "StockSymbol");

            migrationBuilder.CreateIndex(
                name: "IX_US_TradeRecord_TradeDate",
                table: "US_TradeRecord",
                column: "TradeDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "US_TradeRecord");
        }
    }
}
