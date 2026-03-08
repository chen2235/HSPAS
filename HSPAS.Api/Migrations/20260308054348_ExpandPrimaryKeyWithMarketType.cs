using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HSPAS.Api.Migrations
{
    /// <inheritdoc />
    public partial class ExpandPrimaryKeyWithMarketType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_DailyStockPrice",
                table: "DailyStockPrice");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DailyStockPrice",
                table: "DailyStockPrice",
                columns: new[] { "TradeDate", "StockId", "MarketType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_DailyStockPrice",
                table: "DailyStockPrice");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DailyStockPrice",
                table: "DailyStockPrice",
                columns: new[] { "TradeDate", "StockId" });
        }
    }
}
