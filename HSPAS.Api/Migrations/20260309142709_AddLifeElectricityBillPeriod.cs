using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HSPAS.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddLifeElectricityBillPeriod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Life_ElectricityBillPeriod",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Address = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PowerNo = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    BlackoutGroup = table.Column<string>(type: "char(1)", maxLength: 1, nullable: true),
                    BillingStartDate = table.Column<DateTime>(type: "date", nullable: false),
                    BillingEndDate = table.Column<DateTime>(type: "date", nullable: false),
                    BillingDays = table.Column<int>(type: "int", nullable: false),
                    BillingPeriodText = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReadOrDebitDate = table.Column<DateTime>(type: "date", nullable: false),
                    Kwh = table.Column<int>(type: "int", nullable: false),
                    KwhPerDay = table.Column<decimal>(type: "decimal(9,2)", nullable: true),
                    AvgPricePerKwh = table.Column<decimal>(type: "decimal(9,4)", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "decimal(19,2)", nullable: false),
                    InvoiceAmount = table.Column<decimal>(type: "decimal(19,2)", nullable: true),
                    TariffType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SharedMeterHouseholdCount = table.Column<int>(type: "int", nullable: true),
                    InvoicePeriod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    InvoiceNo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    RawDetailJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreateTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdateTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Life_ElectricityBillPeriod", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Life_ElectricityBillPeriod_PowerNo_BillingEndDate",
                table: "Life_ElectricityBillPeriod",
                columns: new[] { "PowerNo", "BillingEndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Life_ElectricityBillPeriod_PowerNo_ReadOrDebitDate",
                table: "Life_ElectricityBillPeriod",
                columns: new[] { "PowerNo", "ReadOrDebitDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Life_ElectricityBillPeriod");
        }
    }
}
