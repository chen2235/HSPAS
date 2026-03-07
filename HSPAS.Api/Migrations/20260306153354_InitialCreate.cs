using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HSPAS.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DailyStockPrice",
                columns: table => new
                {
                    TradeDate = table.Column<DateTime>(type: "date", nullable: false),
                    StockId = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    StockName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TradeVolume = table.Column<long>(type: "bigint", nullable: true),
                    TradeValue = table.Column<long>(type: "bigint", nullable: true),
                    OpenPrice = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    HighPrice = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    LowPrice = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    ClosePrice = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    PriceChange = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    Transaction = table.Column<int>(type: "int", nullable: true),
                    CreateTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyStockPrice", x => new { x.TradeDate, x.StockId });
                });

            migrationBuilder.CreateTable(
                name: "DcaPlan",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlanName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StockId = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    StockName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StartDate = table.Column<DateTime>(type: "date", nullable: false),
                    EndDate = table.Column<DateTime>(type: "date", nullable: true),
                    CycleType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CycleDay = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreateTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DcaPlan", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EtfInfo",
                columns: table => new
                {
                    EtfId = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    EtfName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Issuer = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EtfInfo", x => x.EtfId);
                });

            migrationBuilder.CreateTable(
                name: "TradeRecord",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TradeDate = table.Column<DateTime>(type: "date", nullable: false),
                    StockId = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    StockName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Fee = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Tax = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    OtherCost = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    NetAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreateTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradeRecord", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DcaExecution",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlanId = table.Column<long>(type: "bigint", nullable: false),
                    TradeDate = table.Column<DateTime>(type: "date", nullable: false),
                    StockId = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Fee = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Tax = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    OtherCost = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    NetAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreateTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DcaExecution", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DcaExecution_DcaPlan_PlanId",
                        column: x => x.PlanId,
                        principalTable: "DcaPlan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DcaExecution_PlanId",
                table: "DcaExecution",
                column: "PlanId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyStockPrice");

            migrationBuilder.DropTable(
                name: "DcaExecution");

            migrationBuilder.DropTable(
                name: "EtfInfo");

            migrationBuilder.DropTable(
                name: "TradeRecord");

            migrationBuilder.DropTable(
                name: "DcaPlan");
        }
    }
}
