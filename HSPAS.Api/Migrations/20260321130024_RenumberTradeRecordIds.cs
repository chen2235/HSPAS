using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HSPAS.Api.Migrations
{
    /// <inheritdoc />
    public partial class RenumberTradeRecordIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 將 Id >= 10034 的紀錄從 46 開始重新編號
            // SQL Server 不允許直接 UPDATE IDENTITY 欄位，需透過 INSERT + DELETE
            migrationBuilder.Sql(@"
                -- 1. 建立暫存表，計算新 Id（從 46 開始，依原 Id 排序）
                SELECT
                    ROW_NUMBER() OVER (ORDER BY Id) + 45 AS NewId,
                    Id AS OldId,
                    TradeDate, StockId, StockName, [Action], Quantity,
                    Price, Fee, Tax, OtherCost, NetAmount, Note, CreateTime
                INTO #TempRenumber
                FROM TradeRecord
                WHERE Id >= 10034;

                -- 2. 刪除舊紀錄
                DELETE FROM TradeRecord WHERE Id >= 10034;

                -- 3. 開啟 IDENTITY_INSERT，插入新編號的紀錄
                SET IDENTITY_INSERT TradeRecord ON;

                INSERT INTO TradeRecord (Id, TradeDate, StockId, StockName, [Action], Quantity,
                    Price, Fee, Tax, OtherCost, NetAmount, Note, CreateTime)
                SELECT NewId, TradeDate, StockId, StockName, [Action], Quantity,
                    Price, Fee, Tax, OtherCost, NetAmount, Note, CreateTime
                FROM #TempRenumber
                ORDER BY NewId;

                SET IDENTITY_INSERT TradeRecord OFF;

                -- 4. 重新設定 IDENTITY 種子為目前最大 Id
                DECLARE @maxId BIGINT = (SELECT ISNULL(MAX(Id), 0) FROM TradeRecord);
                DBCC CHECKIDENT ('TradeRecord', RESEED, @maxId);

                -- 5. 清除暫存表
                DROP TABLE #TempRenumber;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 不可逆操作，無法還原原始編號
        }
    }
}
