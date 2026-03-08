using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HSPAS.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddQuarterHealthReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QuarterHealthReport",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportDate = table.Column<DateTime>(type: "date", nullable: false),
                    HospitalName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SourceFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true),
                    SourceFilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OcrJsonRaw = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuarterHealthReport", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QuarterHealthReportDetail",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportId = table.Column<long>(type: "bigint", nullable: false),
                    TCholesterol = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    Triglyceride = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    HDL = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    SGPT_ALT = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    Creatinine = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    UricAcid = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    MDRD_EGFR = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    CKDEPI_EGFR = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    AcSugar = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    Hba1c = table.Column<decimal>(type: "decimal(4,2)", nullable: true),
                    TriglycerideHigh = table.Column<bool>(type: "bit", nullable: true),
                    HDLLow = table.Column<bool>(type: "bit", nullable: true),
                    AcSugarHigh = table.Column<bool>(type: "bit", nullable: true),
                    Hba1cHigh = table.Column<bool>(type: "bit", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuarterHealthReportDetail", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuarterHealthReportDetail_QuarterHealthReport_ReportId",
                        column: x => x.ReportId,
                        principalTable: "QuarterHealthReport",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuarterHealthReportDetail_ReportId",
                table: "QuarterHealthReportDetail",
                column: "ReportId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuarterHealthReportDetail");

            migrationBuilder.DropTable(
                name: "QuarterHealthReport");
        }
    }
}
