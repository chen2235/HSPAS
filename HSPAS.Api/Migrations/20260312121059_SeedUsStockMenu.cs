using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HSPAS.Api.Migrations
{
    /// <inheritdoc />
    public partial class SeedUsStockMenu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
-- 美股投資 Level 2 (under STOCK_ROOT)
INSERT INTO MenuFunction (ParentId, Level, FuncCode, DisplayName, RouteUrl, SortOrder, IsActive, Remark, CreateTime)
SELECT Id, 2, 'US_STOCK', N'美股投資', NULL, 20, 1, N'美股複委託投資管理', SYSUTCDATETIME()
FROM MenuFunction WHERE FuncCode = 'STOCK_ROOT';

-- 美股儀表板 Level 3
INSERT INTO MenuFunction (ParentId, Level, FuncCode, DisplayName, RouteUrl, SortOrder, IsActive, Remark, CreateTime)
SELECT Id, 3, 'US_STOCK_DASH', N'美股儀表板', '/us/dashboard', 1, 1, N'美股投資組合總覽', SYSUTCDATETIME()
FROM MenuFunction WHERE FuncCode = 'US_STOCK';

-- 美股交易紀錄管理 Level 3
INSERT INTO MenuFunction (ParentId, Level, FuncCode, DisplayName, RouteUrl, SortOrder, IsActive, Remark, CreateTime)
SELECT Id, 3, 'US_STOCK_TRD', N'美股交易紀錄', '/us/trades', 2, 1, N'美股交易紀錄管理', SYSUTCDATETIME()
FROM MenuFunction WHERE FuncCode = 'US_STOCK';

-- 美股損益與成本查詢 Level 3
INSERT INTO MenuFunction (ParentId, Level, FuncCode, DisplayName, RouteUrl, SortOrder, IsActive, Remark, CreateTime)
SELECT Id, 3, 'US_STOCK_PNL', N'美股損益查詢', '/us/pnl', 3, 1, N'美股損益與成本查詢', SYSUTCDATETIME()
FROM MenuFunction WHERE FuncCode = 'US_STOCK';
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DELETE FROM MenuFunction WHERE FuncCode IN ('US_STOCK_PNL', 'US_STOCK_TRD', 'US_STOCK_DASH', 'US_STOCK');
");
        }
    }
}
