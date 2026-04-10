-- Business schema for a single-company, multi-branch system

IF OBJECT_ID(N'[BranchSubmissions]', N'U') IS NOT NULL DROP TABLE [BranchSubmissions];
IF OBJECT_ID(N'[ActivityLogs]', N'U') IS NOT NULL DROP TABLE [ActivityLogs];
IF OBJECT_ID(N'[ReportSchedules]', N'U') IS NOT NULL DROP TABLE [ReportSchedules];
IF OBJECT_ID(N'[GoalTargets]', N'U') IS NOT NULL DROP TABLE [GoalTargets];
IF OBJECT_ID(N'[KpiThresholds]', N'U') IS NOT NULL DROP TABLE [KpiThresholds];
IF OBJECT_ID(N'[Sales]', N'U') IS NOT NULL DROP TABLE [Sales];
IF OBJECT_ID(N'[Products]', N'U') IS NOT NULL DROP TABLE [Products];
IF OBJECT_ID(N'[Expenses]', N'U') IS NOT NULL DROP TABLE [Expenses];
IF OBJECT_ID(N'[Branches]', N'U') IS NOT NULL DROP TABLE [Branches];
GO

CREATE TABLE [Branches] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [Address] nvarchar(150) NOT NULL,
    [IsActive] bit NOT NULL,
    [BranchCode] nvarchar(30) NULL,
    CONSTRAINT [PK_Branches] PRIMARY KEY ([Id]),
    CONSTRAINT [UQ_Branches_BranchCode] UNIQUE ([BranchCode])
);
GO

CREATE TABLE [Expenses] (
    [Id] uniqueidentifier NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [Category] nvarchar(max) NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [Date] datetime2 NOT NULL,
    [BranchId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_Expenses] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Expenses_Branches_BranchId]
        FOREIGN KEY ([BranchId]) REFERENCES [Branches]([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [Products] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [Category] nvarchar(max) NOT NULL,
    [Price] decimal(18,2) NOT NULL,
    [StockQuantity] int NOT NULL,
    [LowStockThreshold] int NOT NULL,
    [BranchId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_Products] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Products_Branches_BranchId]
        FOREIGN KEY ([BranchId]) REFERENCES [Branches]([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [Sales] (
    [Id] uniqueidentifier NOT NULL,
    [OrderId] nvarchar(max) NOT NULL,
    [CustomerName] nvarchar(max) NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [Date] datetime2 NOT NULL,
    [BranchId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_Sales] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Sales_Branches_BranchId]
        FOREIGN KEY ([BranchId]) REFERENCES [Branches]([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [KpiThresholds] (
    [Id] uniqueidentifier NOT NULL,
    [MinProfitMargin] decimal(5,2) NOT NULL,
    [MaxExpenseRatio] decimal(5,2) NOT NULL,
    [MinMonthlyProfit] decimal(18,2) NOT NULL,
    [RiskAlertLevel] nvarchar(20) NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    [IsActive] bit NOT NULL,
    [BranchId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_KpiThresholds] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_KpiThresholds_Branches_BranchId]
        FOREIGN KEY ([BranchId]) REFERENCES [Branches]([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [GoalTargets] (
    [Id] uniqueidentifier NOT NULL,
    [MetricName] nvarchar(100) NOT NULL,
    [TargetValue] decimal(18,2) NOT NULL,
    [PeriodType] nvarchar(20) NOT NULL,
    [EffectiveFrom] datetime2 NOT NULL,
    [EffectiveTo] datetime2 NULL,
    [IsActive] bit NOT NULL,
    [BranchId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_GoalTargets] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_GoalTargets_Branches_BranchId]
        FOREIGN KEY ([BranchId]) REFERENCES [Branches]([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [ReportSchedules] (
    [Id] uniqueidentifier NOT NULL,
    [IsEnabled] bit NOT NULL,
    [Frequency] nvarchar(20) NOT NULL,
    [DayOfWeek] int NULL,
    [ScheduledTime] time NOT NULL,
    [Recipients] nvarchar(max) NOT NULL,
    [ReportTypes] nvarchar(max) NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    [BranchId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_ReportSchedules] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ReportSchedules_Branches_BranchId]
        FOREIGN KEY ([BranchId]) REFERENCES [Branches]([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [ActivityLogs] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] nvarchar(450) NULL,
    [UserName] nvarchar(100) NOT NULL,
    [UserRole] nvarchar(50) NOT NULL,
    [ActionType] nvarchar(50) NOT NULL,
    [Description] nvarchar(500) NOT NULL,
    [IpAddress] nvarchar(45) NOT NULL,
    [Location] nvarchar(150) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [BranchId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_ActivityLogs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ActivityLogs_Branches_BranchId]
        FOREIGN KEY ([BranchId]) REFERENCES [Branches]([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [BranchSubmissions] (
    [Id] uniqueidentifier NOT NULL,
    [BranchId] uniqueidentifier NOT NULL,
    [SubmissionYear] int NOT NULL,
    [SubmissionMonth] int NOT NULL,
    [SubmittedByUserId] nvarchar(450) NULL,
    [SubmittedAt] datetime2 NULL,
    [Status] nvarchar(20) NOT NULL,
    [Notes] nvarchar(500) NOT NULL,
    CONSTRAINT [PK_BranchSubmissions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BranchSubmissions_Branches_BranchId]
        FOREIGN KEY ([BranchId]) REFERENCES [Branches]([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_Expenses_BranchId] ON [Expenses]([BranchId]);
CREATE INDEX [IX_Products_BranchId] ON [Products]([BranchId]);
CREATE INDEX [IX_Sales_BranchId] ON [Sales]([BranchId]);
CREATE INDEX [IX_KpiThresholds_BranchId_IsActive] ON [KpiThresholds]([BranchId], [IsActive]);
CREATE INDEX [IX_GoalTargets_BranchId_MetricName_IsActive] ON [GoalTargets]([BranchId], [MetricName], [IsActive]);
CREATE INDEX [IX_ReportSchedules_BranchId_IsEnabled] ON [ReportSchedules]([BranchId], [IsEnabled]);
CREATE INDEX [IX_ActivityLogs_BranchId_CreatedAt] ON [ActivityLogs]([BranchId], [CreatedAt]);
CREATE INDEX [IX_BranchSubmissions_BranchId_Year_Month] ON [BranchSubmissions]([BranchId], [SubmissionYear], [SubmissionMonth]);
GO

