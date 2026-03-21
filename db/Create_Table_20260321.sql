-- ============================================================
-- DailyStockPrice
-- ============================================================
CREATE TABLE [dbo].[DailyStockPrice] (
    [TradeDate] DATE NOT NULL,
    [StockId] NVARCHAR(10) NOT NULL,
    [StockName] NVARCHAR(50) NOT NULL,
    [TradeVolume] BIGINT NULL,
    [TradeValue] DECIMAL(19,4) NULL,
    [OpenPrice] DECIMAL(19,4) NULL,
    [HighPrice] DECIMAL(19,4) NULL,
    [LowPrice] DECIMAL(19,4) NULL,
    [ClosePrice] DECIMAL(19,4) NULL,
    [PriceChange] DECIMAL(19,4) NULL,
    [Transaction] INT NULL,
    [CreateTime] DATETIME2 NOT NULL DEFAULT (sysutcdatetime()),
    [MarketType] NVARCHAR(5) NOT NULL DEFAULT (N'TSE'),
    CONSTRAINT [PK_DailyStockPrice] PRIMARY KEY CLUSTERED ([TradeDate], [StockId], [MarketType])
);


GO

-- ============================================================
-- DcaExecution
-- ============================================================
CREATE TABLE [dbo].[DcaExecution] (
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [PlanId] BIGINT NOT NULL,
    [TradeDate] DATE NOT NULL,
    [StockId] NVARCHAR(10) NOT NULL,
    [Quantity] INT NOT NULL,
    [Price] DECIMAL(19,4) NOT NULL,
    [Fee] DECIMAL(19,4) NOT NULL,
    [Tax] DECIMAL(19,4) NOT NULL,
    [OtherCost] DECIMAL(19,4) NULL,
    [NetAmount] DECIMAL(19,4) NOT NULL,
    [Status] NVARCHAR(20) NOT NULL,
    [Note] NVARCHAR(200) NULL,
    [CreateTime] DATETIME2 NOT NULL DEFAULT (sysutcdatetime()),
    CONSTRAINT [PK_DcaExecution] PRIMARY KEY CLUSTERED ([Id])
);

CREATE NONCLUSTERED INDEX [IX_DcaExecution_PlanId] ON [dbo].[DcaExecution] ([PlanId] ASC);

ALTER TABLE [dbo].[DcaExecution] ADD CONSTRAINT [FK_DcaExecution_DcaPlan_PlanId] FOREIGN KEY ([PlanId]) REFERENCES [dbo].[DcaPlan] ([Id]) ON DELETE CASCADE;

GO

-- ============================================================
-- DcaPlan
-- ============================================================
CREATE TABLE [dbo].[DcaPlan] (
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [PlanName] NVARCHAR(100) NOT NULL,
    [StockId] NVARCHAR(10) NOT NULL,
    [StockName] NVARCHAR(50) NOT NULL,
    [StartDate] DATE NOT NULL,
    [EndDate] DATE NULL,
    [CycleType] NVARCHAR(20) NOT NULL,
    [CycleDay] INT NOT NULL,
    [Amount] DECIMAL(19,4) NOT NULL,
    [IsActive] BIT NOT NULL,
    [Note] NVARCHAR(200) NULL,
    [CreateTime] DATETIME2 NOT NULL DEFAULT (sysutcdatetime()),
    CONSTRAINT [PK_DcaPlan] PRIMARY KEY CLUSTERED ([Id])
);


GO

-- ============================================================
-- EtfInfo
-- ============================================================
CREATE TABLE [dbo].[EtfInfo] (
    [EtfId] NVARCHAR(10) NOT NULL,
    [EtfName] NVARCHAR(100) NOT NULL,
    [Category] NVARCHAR(50) NOT NULL DEFAULT (N''),
    [Issuer] NVARCHAR(100) NULL,
    [IsActive] BIT NOT NULL,
    [CreateTime] DATETIME2 NOT NULL DEFAULT (sysutcdatetime()),
    CONSTRAINT [PK_EtfInfo] PRIMARY KEY CLUSTERED ([EtfId])
);


GO

-- ============================================================
-- Life_ElectricityBillPeriod
-- ============================================================
CREATE TABLE [dbo].[Life_ElectricityBillPeriod] (
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [Address] NVARCHAR(200) NOT NULL,
    [PowerNo] VARCHAR(20) NOT NULL,
    [BlackoutGroup] CHAR(1) NULL,
    [BillingStartDate] DATE NOT NULL,
    [BillingEndDate] DATE NOT NULL,
    [BillingDays] INT NOT NULL,
    [BillingPeriodText] NVARCHAR(100) NULL,
    [ReadOrDebitDate] DATE NOT NULL,
    [Kwh] INT NOT NULL,
    [KwhPerDay] DECIMAL(9,2) NULL,
    [AvgPricePerKwh] DECIMAL(9,4) NULL,
    [TotalAmount] DECIMAL(19,2) NOT NULL,
    [InvoiceAmount] DECIMAL(19,2) NULL,
    [TariffType] NVARCHAR(100) NULL,
    [SharedMeterHouseholdCount] INT NULL,
    [InvoicePeriod] NVARCHAR(50) NULL,
    [InvoiceNo] NVARCHAR(20) NULL,
    [RawDetailJson] NVARCHAR(MAX) NULL,
    [Remark] NVARCHAR(500) NULL,
    [CreateTime] DATETIME2 NOT NULL DEFAULT (sysutcdatetime()),
    [UpdateTime] DATETIME2 NULL,
    CONSTRAINT [PK_Life_ElectricityBillPeriod] PRIMARY KEY CLUSTERED ([Id])
);

CREATE NONCLUSTERED INDEX [IX_Life_ElectricityBillPeriod_PowerNo_BillingEndDate] ON [dbo].[Life_ElectricityBillPeriod] ([PowerNo] ASC, [BillingEndDate] ASC);
CREATE NONCLUSTERED INDEX [IX_Life_ElectricityBillPeriod_PowerNo_ReadOrDebitDate] ON [dbo].[Life_ElectricityBillPeriod] ([PowerNo] ASC, [ReadOrDebitDate] ASC);

GO

-- ============================================================
-- Life_WaterBillPeriod
-- ============================================================
CREATE TABLE [dbo].[Life_WaterBillPeriod] (
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [WaterAddress] NVARCHAR(200) NOT NULL,
    [WaterNo] VARCHAR(20) NOT NULL,
    [MeterNo] VARCHAR(30) NOT NULL,
    [BillingStartDate] DATE NOT NULL,
    [BillingEndDate] DATE NOT NULL,
    [BillingDays] INT NULL,
    [BillingPeriodText] NVARCHAR(100) NULL,
    [TotalUsage] INT NULL,
    [CurrentUsage] INT NOT NULL,
    [CurrentMeterReading] INT NOT NULL,
    [PreviousMeterReading] INT NOT NULL,
    [TotalAmount] DECIMAL(19,2) NOT NULL,
    [InvoiceAmount] DECIMAL(19,2) NULL,
    [TariffType] NVARCHAR(100) NULL,
    [RawDetailJson] NVARCHAR(MAX) NULL,
    [Remark] NVARCHAR(500) NULL,
    [CreateTime] DATETIME2 NOT NULL DEFAULT (sysutcdatetime()),
    [UpdateTime] DATETIME2 NULL,
    CONSTRAINT [PK_Life_WaterBillPeriod] PRIMARY KEY CLUSTERED ([Id])
);

CREATE NONCLUSTERED INDEX [IX_Life_WaterBillPeriod_WaterNo_BillingEndDate] ON [dbo].[Life_WaterBillPeriod] ([WaterNo] ASC, [BillingEndDate] ASC);

GO

-- ============================================================
-- MenuFunction
-- ============================================================
CREATE TABLE [dbo].[MenuFunction] (
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [ParentId] BIGINT NULL,
    [Level] INT NOT NULL,
    [FuncCode] NVARCHAR(50) NOT NULL,
    [DisplayName] NVARCHAR(100) NOT NULL,
    [RouteUrl] NVARCHAR(200) NULL,
    [SortOrder] INT NOT NULL,
    [IsActive] BIT NOT NULL DEFAULT (CONVERT([bit],(1))),
    [Remark] NVARCHAR(200) NULL,
    [CreateTime] DATETIME2 NOT NULL DEFAULT (sysutcdatetime()),
    CONSTRAINT [PK_MenuFunction] PRIMARY KEY CLUSTERED ([Id])
);

CREATE UNIQUE NONCLUSTERED INDEX [IX_MenuFunction_FuncCode] ON [dbo].[MenuFunction] ([FuncCode] ASC);

GO

-- ============================================================
-- QuarterHealthReport
-- ============================================================
CREATE TABLE [dbo].[QuarterHealthReport] (
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [ReportDate] DATE NOT NULL,
    [HospitalName] NVARCHAR(100) NULL,
    [SourceFileName] NVARCHAR(260) NULL,
    [SourceFilePath] NVARCHAR(500) NULL,
    [OcrJsonRaw] NVARCHAR(MAX) NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT (sysutcdatetime()),
    [UpdatedAt] DATETIME2 NULL,
    CONSTRAINT [PK_QuarterHealthReport] PRIMARY KEY CLUSTERED ([Id])
);


GO

-- ============================================================
-- QuarterHealthReportDetail
-- ============================================================
CREATE TABLE [dbo].[QuarterHealthReportDetail] (
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [ReportId] BIGINT NOT NULL,
    [TCholesterol] DECIMAL(6,2) NULL,
    [Triglyceride] DECIMAL(6,2) NULL,
    [HDL] DECIMAL(6,2) NULL,
    [SGPT_ALT] DECIMAL(6,2) NULL,
    [Creatinine] DECIMAL(6,2) NULL,
    [UricAcid] DECIMAL(6,2) NULL,
    [MDRD_EGFR] DECIMAL(6,2) NULL,
    [CKDEPI_EGFR] DECIMAL(6,2) NULL,
    [AcSugar] DECIMAL(6,2) NULL,
    [Hba1c] DECIMAL(4,2) NULL,
    [TriglycerideHigh] BIT NULL,
    [HDLLow] BIT NULL,
    [AcSugarHigh] BIT NULL,
    [Hba1cHigh] BIT NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT (sysutcdatetime()),
    CONSTRAINT [PK_QuarterHealthReportDetail] PRIMARY KEY CLUSTERED ([Id])
);

CREATE UNIQUE NONCLUSTERED INDEX [IX_QuarterHealthReportDetail_ReportId] ON [dbo].[QuarterHealthReportDetail] ([ReportId] ASC);

ALTER TABLE [dbo].[QuarterHealthReportDetail] ADD CONSTRAINT [FK_QuarterHealthReportDetail_QuarterHealthReport_ReportId] FOREIGN KEY ([ReportId]) REFERENCES [dbo].[QuarterHealthReport] ([Id]) ON DELETE CASCADE;

GO

-- ============================================================
-- TradeRecord
-- ============================================================
CREATE TABLE [dbo].[TradeRecord] (
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [TradeDate] DATE NOT NULL,
    [StockId] NVARCHAR(10) NOT NULL,
    [StockName] NVARCHAR(50) NOT NULL,
    [Action] NVARCHAR(10) NOT NULL,
    [Quantity] INT NOT NULL,
    [Price] DECIMAL(19,4) NOT NULL,
    [Fee] DECIMAL(19,4) NOT NULL,
    [Tax] DECIMAL(19,4) NOT NULL,
    [OtherCost] DECIMAL(19,4) NULL,
    [NetAmount] DECIMAL(19,4) NOT NULL,
    [Note] NVARCHAR(200) NULL,
    [CreateTime] DATETIME2 NOT NULL DEFAULT (sysutcdatetime()),
    CONSTRAINT [PK_TradeRecord] PRIMARY KEY CLUSTERED ([Id])
);


GO

-- ============================================================
-- US_TradeRecord
-- ============================================================
CREATE TABLE [dbo].[US_TradeRecord] (
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [TradeDate] DATE NOT NULL,
    [SettlementDate] DATE NULL,
    [StockSymbol] NVARCHAR(20) NOT NULL,
    [StockName] NVARCHAR(100) NOT NULL,
    [Market] NVARCHAR(20) NOT NULL,
    [Action] NVARCHAR(10) NOT NULL,
    [Currency] NVARCHAR(5) NOT NULL,
    [Quantity] DECIMAL(19,6) NOT NULL,
    [Price] DECIMAL(19,6) NOT NULL,
    [Amount] DECIMAL(19,4) NOT NULL,
    [Fee] DECIMAL(19,4) NOT NULL,
    [Tax] DECIMAL(19,4) NOT NULL,
    [NetAmount] DECIMAL(19,4) NOT NULL,
    [SettlementCurrency] NVARCHAR(5) NULL,
    [ExchangeRate] DECIMAL(19,4) NULL,
    [NetAmountTwd] DECIMAL(19,4) NULL,
    [TradeRef] NVARCHAR(20) NULL,
    [Note] NVARCHAR(500) NULL,
    [CreateTime] DATETIME2 NOT NULL DEFAULT (sysutcdatetime()),
    CONSTRAINT [PK_US_TradeRecord] PRIMARY KEY CLUSTERED ([Id])
);

CREATE NONCLUSTERED INDEX [IX_US_TradeRecord_StockSymbol] ON [dbo].[US_TradeRecord] ([StockSymbol] ASC);
CREATE NONCLUSTERED INDEX [IX_US_TradeRecord_TradeDate] ON [dbo].[US_TradeRecord] ([TradeDate] ASC);

GO

