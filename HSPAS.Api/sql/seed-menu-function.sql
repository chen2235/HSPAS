-- MenuFunction 初始資料
-- 執行前確認 MenuFunction 資料表已建立（EF Core Migration）

-- 先清除既有資料（如需重新初始化）
-- DELETE FROM [dbo].[MenuFunction];
-- DBCC CHECKIDENT ('MenuFunction', RESEED, 0);

-- 若已有資料則跳過
IF NOT EXISTS (SELECT 1 FROM [dbo].[MenuFunction])
BEGIN

-- Level 1：股票損益紀錄
INSERT INTO [dbo].[MenuFunction] ([ParentId], [Level], [FuncCode], [DisplayName], [RouteUrl], [SortOrder], [IsActive], [Remark])
VALUES (NULL, 1, 'STOCK_ROOT', N'股票損益紀錄', NULL, 1, 1, N'原 HSPAS 全部功能集中於此');

DECLARE @STOCK_ROOT_ID BIGINT = SCOPE_IDENTITY();

-- Level 2：股票損益分析
INSERT INTO [dbo].[MenuFunction] ([ParentId], [Level], [FuncCode], [DisplayName], [RouteUrl], [SortOrder], [IsActive], [Remark])
VALUES (@STOCK_ROOT_ID, 2, 'STOCK_ANALYSIS', N'股票損益分析', NULL, 1, 1, N'底下放原有股票功能');

DECLARE @STOCK_ANALYSIS_ID BIGINT = SCOPE_IDENTITY();

-- Level 3：各功能頁
INSERT INTO [dbo].[MenuFunction] ([ParentId], [Level], [FuncCode], [DisplayName], [RouteUrl], [SortOrder], [IsActive], [Remark])
VALUES
(@STOCK_ANALYSIS_ID, 3, 'STOCK_DASH',   N'股票儀表板 Dashboard',   N'/dashboard',                     1, 1, N'原 DASH'),
(@STOCK_ANALYSIS_ID, 3, 'STOCK_CAL',    N'日曆行情查詢',            N'/calendar',                      2, 1, N'原 CAL'),
(@STOCK_ANALYSIS_ID, 3, 'STOCK_STOCK',  N'個股/ETF 查詢入口',       N'/stock',                         3, 1, N'原 STOCK'),
(@STOCK_ANALYSIS_ID, 3, 'STOCK_ETF',    N'ETF 專區',                N'/etf',                           4, 1, N'原 ETF'),
(@STOCK_ANALYSIS_ID, 3, 'STOCK_TRD',    N'交易紀錄管理',            N'/trades',                        5, 1, N'原 TRD'),
(@STOCK_ANALYSIS_ID, 3, 'STOCK_DCA',    N'定期定額管理',            N'/dca',                           6, 1, N'原 DCA'),
(@STOCK_ANALYSIS_ID, 3, 'STOCK_PNL',    N'損益與成本查詢',          N'/pnl',                           7, 1, N'原 PNL'),
(@STOCK_ANALYSIS_ID, 3, 'STOCK_IDEAS',  N'投資建議（長/短期）',     N'/recommendations',              8, 1, N'原 IDEAS'),
(@STOCK_ANALYSIS_ID, 3, 'STOCK_ALERT',  N'風險警示（季線跌破）',    N'/alerts',                        9, 1, N'原 ALERT'),
(@STOCK_ANALYSIS_ID, 3, 'STOCK_HIST',   N'歷史資料回補',            N'/admin/history-backfill',       10, 1, N'原 HIST'),
(@STOCK_ANALYSIS_ID, 3, 'STOCK_CONF',   N'系統設定（預留）',        N'/settings',                     11, 1, N'原 CONF');

-- Level 1：健康管理紀錄
INSERT INTO [dbo].[MenuFunction] ([ParentId], [Level], [FuncCode], [DisplayName], [RouteUrl], [SortOrder], [IsActive], [Remark])
VALUES (NULL, 1, 'HEALTH_ROOT', N'健康管理紀錄', NULL, 2, 1, N'健康相關主功能');

DECLARE @HEALTH_ROOT_ID BIGINT = SCOPE_IDENTITY();

-- Level 2：健檢報告紀錄
INSERT INTO [dbo].[MenuFunction] ([ParentId], [Level], [FuncCode], [DisplayName], [RouteUrl], [SortOrder], [IsActive], [Remark])
VALUES (@HEALTH_ROOT_ID, 2, 'HEALTH_CHECKUP', N'健檢報告紀錄', NULL, 1, 1, N'個人與公司健檢報告');

DECLARE @HEALTH_CHECKUP_ID BIGINT = SCOPE_IDENTITY();

-- Level 3：健檢報告功能
INSERT INTO [dbo].[MenuFunction] ([ParentId], [Level], [FuncCode], [DisplayName], [RouteUrl], [SortOrder], [IsActive], [Remark])
VALUES
(@HEALTH_CHECKUP_ID, 3, 'HEALTH_CHECKUP_QTR_UP',   N'每三個月報告紀錄上傳',    N'/health/checkup/qtr/upload',        1, 1, N'個人每季健檢報告上傳'),
(@HEALTH_CHECKUP_ID, 3, 'HEALTH_CHECKUP_QTR_DASH', N'每三個月報告儀表板',      N'/health/checkup/qtr/dashboard',     2, 1, N'個人每季健檢儀表板'),
(@HEALTH_CHECKUP_ID, 3, 'HEALTH_CHECKUP_CO_UP',    N'公司每年報告紀錄上傳',    N'/health/checkup/company/upload',    3, 1, N'公司年度健檢報告上傳'),
(@HEALTH_CHECKUP_ID, 3, 'HEALTH_CHECKUP_CO_DASH',  N'公司每年報告儀表板',      N'/health/checkup/company/dashboard', 4, 1, N'公司年度健檢儀表板');

-- Level 1：生活計帳
INSERT INTO [dbo].[MenuFunction] ([ParentId], [Level], [FuncCode], [DisplayName], [RouteUrl], [SortOrder], [IsActive], [Remark])
VALUES (NULL, 1, 'LIFE_ROOT', N'生活計帳', NULL, 3, 1, N'生活收支與紀錄');

DECLARE @LIFE_ROOT_ID BIGINT = SCOPE_IDENTITY();

-- Level 2：妹妹紀錄
INSERT INTO [dbo].[MenuFunction] ([ParentId], [Level], [FuncCode], [DisplayName], [RouteUrl], [SortOrder], [IsActive], [Remark])
VALUES (@LIFE_ROOT_ID, 2, 'LIFE_SIS', N'妹妹紀錄', NULL, 1, 1, N'妹妹相關收支與事件紀錄');

DECLARE @LIFE_SIS_ID BIGINT = SCOPE_IDENTITY();

-- Level 3：妹妹紀錄功能
INSERT INTO [dbo].[MenuFunction] ([ParentId], [Level], [FuncCode], [DisplayName], [RouteUrl], [SortOrder], [IsActive], [Remark])
VALUES
(@LIFE_SIS_ID, 3, 'LIFE_SIS_RECORD',          N'妹妹紀錄維護',       N'/life/sister/records',          1, 1, N'基本紀錄維護畫面'),
(@LIFE_SIS_ID, 3, 'LIFE_SIS_YEARLY_ANALYSIS', N'妹妹紀錄年度分析',   N'/life/sister/yearly-analysis',  2, 1, N'年度統計與圖表');

-- Level 1：系統管理作業
INSERT INTO [dbo].[MenuFunction] ([ParentId], [Level], [FuncCode], [DisplayName], [RouteUrl], [SortOrder], [IsActive], [Remark])
VALUES (NULL, 1, 'ADMIN_ROOT', N'系統管理作業', NULL, 4, 1, N'系統管理相關功能');

DECLARE @ADMIN_ROOT_ID BIGINT = SCOPE_IDENTITY();

-- Level 2：功能管理
INSERT INTO [dbo].[MenuFunction] ([ParentId], [Level], [FuncCode], [DisplayName], [RouteUrl], [SortOrder], [IsActive], [Remark])
VALUES (@ADMIN_ROOT_ID, 2, 'ADMIN_FUNC', N'功能管理', NULL, 1, 1, N'功能選單相關管理');

DECLARE @ADMIN_FUNC_ID BIGINT = SCOPE_IDENTITY();

-- Level 3：功能選單排序管理
INSERT INTO [dbo].[MenuFunction] ([ParentId], [Level], [FuncCode], [DisplayName], [RouteUrl], [SortOrder], [IsActive], [Remark])
VALUES (@ADMIN_FUNC_ID, 3, 'ADMIN_MENU_SORT', N'功能選單排序管理', N'/settings/menu-sorting', 1, 1, N'拖拉調整選單排序與階層');

PRINT N'MenuFunction 初始資料已建立完成（含系統管理作業）';
END
ELSE
BEGIN
    PRINT N'MenuFunction 已有資料，跳過初始化';
END
GO
