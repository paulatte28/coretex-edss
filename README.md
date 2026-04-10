# Coretex: Intelligent Executive Decision Support System

> An Executive Decision Support System (EDSS) designed for small to medium enterprises — centralizing multi-branch KPI monitoring, financial analytics, predictive trends, and executive reporting into one secure, role-based platform.

---

## Table of Contents

- [Overview](#overview)
- [System Flow](#system-flow)
- [Modules](#modules)
- [User Roles](#user-roles)
- [Tech Stack](#tech-stack)
- [APIs Used](#apis-used)
- [Security Features](#security-features)
- [Getting Started](#getting-started)
- [Project Structure](#project-structure)
- [Environment Variables](#environment-variables)
- [License](#license)

---

## Overview

Coretex is an Executive Decision Support System (EDSS) built for a single company with multiple branches. It enables the Business Owner/CEO to monitor consolidated KPIs, view predictive analytics, and receive real-time alerts across all branches. Branch Cashiers record daily POS transactions, Finance Officers log monthly expenses with auto-currency conversion, and the System Administrator manages user accounts, KPI thresholds, and scheduled reports.

**Subject:** IT15/L — Integrative Programming and Technologies  
**Course Code:** 8448  
**Developer:** Kristine Paula Saldon Coretico  
**Topic:** #3 Executive Decision Support System (EDSS/ESS)  
**Business Type:** Small to Medium Enterprise with Multi-Branch Operations

---

## System Flow

### Phase 1 — System Admin Setup
System Admin logs in → IPGeolocation logs location → Creates accounts for CEO, Finance Officers, and Branch Cashiers → Sets KPI thresholds (e.g., "alert when profit drops below ₱50,000") → Schedules auto-email reports via SendGrid (e.g., every Monday 8AM)

### Phase 2 — Daily POS Recording (Branch Cashier)
Branch Cashier logs in daily → Opens POS Transaction Module → Records each sale: product name, quantity, unit price → System saves and auto-tags to their branch → Repeated daily throughout the month

### Phase 3 — Monthly Expense Logging (Finance Officer)
Finance Officer logs in at month-end → Logs 4 expense categories: Cost of Goods (typed in USD/CNY → Exchange Rate API auto-converts to PHP), Rent, Salaries, Utilities → All expenses saved and branch-tagged

### Phase 4 — Confirm & Submit to EDSS (Finance Officer)
Finance Officer reviews the auto-aggregated monthly sales total from cashier entries + the expenses they logged → Verifies everything is correct → Clicks Confirm & Submit → Data enters the EDSS pipeline

### Phase 5 — Auto-Processing (System)
Backend instantly computes: Profit Margin, Expense Ratio, Revenue Forecast, Expense Trend, Risk Score (Green/Yellow/Red), Business Health Summary, Branch Performance Comparison → If any KPI breaches threshold → Twilio fires SMS to Business Owner immediately

### Phase 6 — CEO Dashboard
Business Owner logs in → Sees all branches consolidated → KPI Dashboard → Interactive Charts (Chart.js) → Predictive Trend Analysis → Month-over-Month Comparison → Branch Performance Alerts → Decision Support Analytics with live news from NewsAPI → Generates/downloads PDF Executive Report → Receives auto-emailed report every Monday via SendGrid

### Phase 7 — System Admin Monitoring
Reviews activity logs (IPGeolocation tracks every login) → Audit trail shows who changed what data and when → Monitors which branches have submitted → Resets passwords → Adjusts thresholds or schedules

---

## Modules

### 1. POS Transaction Module
Allows Branch Cashiers to record daily sales transactions including product name, quantity, and unit price. All entries are automatically tagged to the cashier's assigned branch.

### 2. Expense Logging Module
Enables Finance Officers to log monthly expenses across four categories: Cost of Goods, Rent, Salaries, and Utilities. Supports multi-currency input (USD/CNY) with automatic conversion to PHP via Exchange Rate API.

### 3. KPI Dashboard Module
Displays real-time key performance indicators across all branches — Profit Margin, Expense Ratio, Revenue Forecast, Risk Score (Green/Yellow/Red), and Business Health Summary. Includes configurable KPI thresholds that trigger automated SMS alerts via Twilio when values are breached.

### 4. Predictive Trend Analysis Module
Generates revenue forecasts, expense trend analysis, and month-over-month comparisons based on historical data. Helps the Business Owner anticipate operational needs and identify patterns.

### 5. Executive Reporting Module
Automates the generation of executive-level PDF reports summarizing business performance. Reports are delivered directly to the Business Owner via email through SendGrid and can be scheduled by the System Administrator (e.g., every Monday 8AM).

### 6. Data Visualization & Insights Module
Presents business data through interactive charts powered by Chart.js. Includes branch performance comparison, drill-down views, and live multi-currency conversion display.

### 7. Decision Support Analytics Module
Provides risk scoring, branch performance alerts, and strategic insights integrated with live business news from NewsAPI. Helps the Business Owner make data-driven decisions.

---

## User Roles

| Role | Scope | Access |
|---|---|---|
| **System Administrator** | Company-wide | User account management, KPI threshold settings, report scheduling, activity logs, audit trail monitoring |
| **Business Owner / CEO** | Company-wide | Full access to KPI Dashboard, Predictive Trends, Executive Reports, Decision Support Analytics |
| **Finance Officer** | Assigned Branch | Expense logging, monthly data review, confirm & submit to EDSS, multi-currency conversion |
| **Branch Cashier** | Assigned Branch | Daily POS transaction recording |

---

## Tech Stack

| Layer | Technology |
|---|---|
| Backend | C#, ASP.NET Core MVC |
| Frontend | Razor Pages, Tailwind CSS, Chart.js |
| Database | SQL Server (managed via SSMS) |
| IDE | Visual Studio 2022 |
| API Testing | Postman |
| Hosting | Domain + Web Hosting |

---

## APIs Used

### 1. Exchange Rate API — [ExchangeRate-API.com](https://www.exchangerate-api.com)
Used in the **Expense Logging Module** to provide live multi-currency conversion. When Finance Officers enter Cost of Goods in USD or CNY, the system automatically converts to PHP using real-time exchange rates.

### 2. SendGrid API — [sendgrid.com](https://sendgrid.com)
Used in the **Executive Reporting Module** to automatically deliver generated PDF reports to the Business Owner/CEO via email on a scheduled basis (e.g., every Monday 8AM) or on-demand.

### 3. Twilio SMS API — [twilio.com](https://www.twilio.com)
Used in the **KPI Dashboard Module** to send real-time SMS alerts directly to the Business Owner when a KPI metric breaches a configured threshold — ensuring critical business changes are communicated immediately.

### 4. IPGeolocation API — [ipgeolocation.io](https://ipgeolocation.io)
Used for **Activity Logging and Audit Trail**. Tracks the location of every user login and logs it for security monitoring by the System Administrator.

### 5. NewsAPI — [newsapi.org](https://newsapi.org)
Used in the **Decision Support Analytics Module** to display live business news relevant to the industry, helping the Business Owner stay informed when making strategic decisions.

---

## Security Features

- **Role-Based Access Control (RBAC)** — each user role has strictly defined access boundaries
- **User Authentication** — secure login system per company account
- **Input Validation** — all user inputs are validated server-side
- **Activity Logging** — all system actions are recorded per branch
- **SSL/HTTPS Encryption** — all data in transit is encrypted
- **Data Encryption at Rest** — sensitive business data is encrypted in the database
- **Session Timeout Management** — inactive sessions are automatically terminated
- **API Key Protection** — all third-party API keys are stored securely via environment variables
- **SMS-Based KPI Threshold Alerts** — real-time notifications via Twilio for critical KPI breaches

---

## Getting Started

### Prerequisites

- Visual Studio 2022
- SQL Server + SQL Server Management Studio (SSMS)
- .NET 8 SDK or later
- Node.js (for Tailwind CSS build)
- Postman (for API testing)

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/coretex-edss.git
   cd coretex-edss
   ```

2. **Restore .NET dependencies**
   ```bash
   dotnet restore
   ```

3. **Install Tailwind CSS**
   ```bash
   npm install
   ```

4. **Set up the database**
   - Open SSMS and connect to your SQL Server instance
   - Run the migration scripts located in `/Database/Scripts/`
   - Or use EF Core migrations:
   ```bash
   dotnet ef database update
   ```

5. **Configure environment variables**
   - Copy `.env.example` to `.env`
   - Fill in your API keys and connection string (see [Environment Variables](#environment-variables))

6. **Run the application**
   ```bash
   dotnet run
   ```

7. **Access the system**
   - Open your browser and go to `https://localhost:5001`

---

## Project Structure

```
coretex-edss/
│
├── Controllers/                  # ASP.NET Core MVC Controllers
│   ├── DashboardController.cs
│   ├── ReportingController.cs
│   ├── AnalyticsController.cs
│   └── AccountController.cs
│
├── Models/                       # Data models
│   ├── KPI.cs
│   ├── Report.cs
│   └── User.cs
│
├── Views/                        # Razor Pages views
│   ├── Dashboard/
│   ├── Reporting/
│   ├── Analytics/
│   ├── DataVisualization/
│   ├── DecisionSupport/
│
├── Services/                     # Business logic & API integrations
│   ├── ExchangeRateService.cs
│   ├── SendGridService.cs
│   ├── TwilioService.cs
│   └── ReportService.cs
│
├── Data/                         # Database context & repositories
│   └── AppDbContext.cs
│
├── Database/
│   └── Scripts/                  # SQL migration scripts
│
├── wwwroot/                      # Static files
│   ├── css/
│   ├── js/
│   └── images/
│
├── appsettings.json              # App configuration
├── appsettings.Development.json
└── README.md
```

---

## Environment Variables

Create an `appsettings.Development.json` or use environment variables for the following:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=CoretexDB;Trusted_Connection=True;"
  },
  "ExchangeRateAPI": {
    "BaseUrl": "https://v6.exchangerate-api.com/v6/",
    "ApiKey": "YOUR_EXCHANGE_RATE_API_KEY"
  },
  "SendGrid": {
    "ApiKey": "YOUR_SENDGRID_API_KEY",
    "SenderEmail": "noreply@yourdomain.com",
    "SenderName": "Coretex EDSS"
  },
  "Twilio": {
    "AccountSid": "YOUR_TWILIO_ACCOUNT_SID",
    "AuthToken": "YOUR_TWILIO_AUTH_TOKEN",
    "FromNumber": "+1XXXXXXXXXX"
  }
}
```

> **Never commit API keys or connection strings to version control.** Add `appsettings.Development.json` to your `.gitignore`.

---

## License

This project is developed as an academic requirement for **IT15/L — Integrative Programming and Technologies**.  
All rights reserved © Kristine Paula Saldon Coretico.
