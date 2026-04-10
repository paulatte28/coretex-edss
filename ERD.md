# ERD (Business-Only, Planned + Implemented)

Scope for final documentation:
- Includes business-domain tables only.
- Excludes ASP.NET Identity/framework tables.

```mermaid
erDiagram
    Branches {
        guid Id PK
        string Name
        string Address
        bool IsActive
        string BranchCode
    }

    Products {
        guid Id PK
        string Name
        string Category
        decimal Price
        int StockQuantity
        int LowStockThreshold
           guid BranchId FK
    }

    Sales {
        guid Id PK
        string OrderId
        string CustomerName
        decimal Amount
        string Status
        datetime Date
           guid BranchId FK
    }

    Expenses {
        guid Id PK
        string Description
        string Category
        decimal Amount
        datetime Date
           guid BranchId FK
    }

    KpiThresholds {
        guid Id PK
        decimal MinProfitMargin
        decimal MaxExpenseRatio
        decimal MinMonthlyProfit
        string RiskAlertLevel
        datetime UpdatedAt
        bool IsActive
           guid BranchId FK
    }

    GoalTargets {
        guid Id PK
        string MetricName
        decimal TargetValue
        string PeriodType
        datetime EffectiveFrom
        datetime EffectiveTo
        bool IsActive
           guid BranchId FK
    }

    ReportSchedules {
        guid Id PK
        bool IsEnabled
        string Frequency
        int DayOfWeek
        time ScheduledTime
        string Recipients
        string ReportTypes
        datetime UpdatedAt
           guid BranchId FK
    }

    ActivityLogs {
        guid Id PK
        string UserId
        string UserName
        string UserRole
        string ActionType
        string Description
        string IpAddress
        string Location
        datetime CreatedAt
           guid BranchId FK
    }

    BranchSubmissions {
        guid Id PK
        guid BranchId FK
        int SubmissionYear
        int SubmissionMonth
        string SubmittedByUserId
        datetime SubmittedAt
        string Status
        string Notes
    }

    Branches ||--o{ BranchSubmissions : BranchId
    Branches ||--o{ Products : BranchId
    Branches ||--o{ Sales : BranchId
    Branches ||--o{ Expenses : BranchId
    Branches ||--o{ KpiThresholds : BranchId
    Branches ||--o{ GoalTargets : BranchId
    Branches ||--o{ ReportSchedules : BranchId
    Branches ||--o{ ActivityLogs : BranchId
```

## Notes
- This ERD is aligned with [schema.sql](schema.sql).
- Existing implemented transaction tables remain unchanged.
- Added admin-process and monitoring tables to match the planned system flow.
