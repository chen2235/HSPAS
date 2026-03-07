-- HSPAS 資料庫初始化腳本
-- 用途：建立資料庫 HSPAS 與使用者帳號 hspasmgr
-- 執行方式：以 sa 或有足夠權限的帳號在 SQL Server Management Studio 中執行

-- 1. 建立資料庫
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'HSPAS')
BEGIN
    CREATE DATABASE [HSPAS];
    PRINT N'資料庫 HSPAS 已建立。';
END
ELSE
    PRINT N'資料庫 HSPAS 已存在，略過建立。';
GO

-- 2. 建立登入帳號
IF NOT EXISTS (SELECT name FROM sys.server_principals WHERE name = N'hspasmgr')
BEGIN
    CREATE LOGIN [hspasmgr] WITH PASSWORD = N'tvhspasmgr', DEFAULT_DATABASE = [HSPAS];
    PRINT N'登入帳號 hspasmgr 已建立。';
END
ELSE
    PRINT N'登入帳號 hspasmgr 已存在，略過建立。';
GO

-- 3. 在 HSPAS 資料庫中建立使用者並賦予權限
USE [HSPAS];
GO

IF NOT EXISTS (SELECT name FROM sys.database_principals WHERE name = N'hspasmgr')
BEGIN
    CREATE USER [hspasmgr] FOR LOGIN [hspasmgr];
    PRINT N'資料庫使用者 hspasmgr 已建立。';
END
ELSE
    PRINT N'資料庫使用者 hspasmgr 已存在，略過建立。';
GO

-- 賦予 db_owner 角色（開發環境）
ALTER ROLE [db_owner] ADD MEMBER [hspasmgr];
PRINT N'已將 hspasmgr 加入 db_owner 角色。';
GO

PRINT N'HSPAS 資料庫初始化完成。';
PRINT N'連線字串：Server=localhost;Database=HSPAS;User ID=hspasmgr;Password=tvhspasmgr;TrustServerCertificate=True';
GO
