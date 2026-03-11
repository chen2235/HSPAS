using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HSPAS.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddLifeWaterBillPeriod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Life_WaterBillPeriod",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WaterAddress = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    WaterNo = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    MeterNo = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    BillingStartDate = table.Column<DateTime>(type: "date", nullable: false),
                    BillingEndDate = table.Column<DateTime>(type: "date", nullable: false),
                    BillingDays = table.Column<int>(type: "int", nullable: true),
                    BillingPeriodText = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TotalUsage = table.Column<int>(type: "int", nullable: true),
                    CurrentUsage = table.Column<int>(type: "int", nullable: false),
                    CurrentMeterReading = table.Column<int>(type: "int", nullable: false),
                    PreviousMeterReading = table.Column<int>(type: "int", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(19,2)", nullable: false),
                    InvoiceAmount = table.Column<decimal>(type: "decimal(19,2)", nullable: true),
                    TariffType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RawDetailJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreateTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdateTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Life_WaterBillPeriod", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Life_WaterBillPeriod_WaterNo_BillingEndDate",
                table: "Life_WaterBillPeriod",
                columns: new[] { "WaterNo", "BillingEndDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Life_WaterBillPeriod");
        }
    }
}
