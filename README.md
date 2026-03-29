# Coretex: Intelligent Executive Decision Support System

> A SaaS-based multi-tenant Executive Decision Support System (EDSS) designed for small to medium electronics businesses — centralizing KPI monitoring, financial analytics, predictive trends, and executive reporting into one secure, role-based platform.

---

## Table of Contents

- [Overview](#overview)
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

Coretex is a SaaS-based multi-tenant EDSS where multiple small to medium electronics companies can independently subscribe and use the platform. Each subscribed company operates within its own isolated environment — the Super Admin (Platform Manager) can monitor all registered tenants but cannot access, edit, or interfere with any individual company's data.

**Subject:** IT15/L — Integrative Programming and Technologies  
**Course Code:** 8448  
**Developer:** Kristine Paula Saldon Coretico  
**Topic:** #3 Executive Decision Support System (EDSS/ESS)  
**Business Type:** Electronics Products Sales, Inventory Management, and Financial Analytics

---

## Modules

### 1. KPI Dashboard Module
Displays real-time key performance indicators for the business — sales revenue, inventory levels, profit margins, and other critical metrics. Includes configurable KPI thresholds that trigger automated SMS alerts via Twilio when values fall below acceptable levels.

### 2. Predictive Trend Analysis Module
Generates sales forecasts, demand trend analysis, inventory predictions, and seasonal pattern reviews based on historical business data. Helps the business owner anticipate operational needs without manual computation.

### 3. Executive Reporting Module
Automates the generation of executive-level reports summarizing business performance. Reports are delivered directly to the business owner via email through the SendGrid API and can be exported as PDF files. Supports scheduled auto-reports configured by the System Administrator.

### 4. Data Visualization & Insights Module
Presents business data through interactive charts and drill-down views powered by Chart.js. Includes live multi-currency conversion via the Exchange Rate API — essential for electronics businesses sourcing products in foreign currencies (USD, CNY, EUR).

### 5. Decision Support Analytics Module
Provides what-if scenario modeling, risk scoring, pricing decision evaluation, and AI-generated recommendations to help the business owner make structured, data-driven strategic decisions.

### 6. Tenant & Subscription Management Module
Exclusively for the Super Admin. Allows monitoring of all registered electronics companies, activation and deactivation of company accounts, subscription plan management, and system health and log viewing. The Super Admin has **view-only** access — no editing of any company's internal data is permitted.

---

## User Roles

| Role | Scope | Access |
|---|---|---|
| **Super Admin (Platform Manager)** | Platform-wide | Module 6 only — view-only monitoring of all tenants |
| **Business Owner / CEO** | Own company | Modules 1–5 — full access to all EDSS features |
| **Finance Officer** | Own company | Modules 3 & 4 — reports, budget validation, multi-currency conversion |
| **System Administrator** | Own company | User management, system settings, report scheduling, activity logs, KPI alert rules |

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
Used in the **Data Visualization & Insights Module** to provide live multi-currency conversion. Electronics businesses frequently source products priced in foreign currencies. Rates are fetched server-side and cached to minimize API calls.

### 2. SendGrid API — [sendgrid.com](https://sendgrid.com)
Used in the **Executive Reporting Module** to automatically deliver generated PDF reports to the Business Owner/CEO via email on a scheduled or on-demand basis.

### 3. Twilio SMS API — [twilio.com](https://www.twilio.com)
Used in the **KPI Dashboard Module** to send real-time SMS alerts directly to the business owner when a KPI metric falls below a configured threshold — ensuring critical business changes are communicated even without logging into the system.

---

## Security Features

- **Role-Based Access Control (RBAC)** — each user role has strictly defined access boundaries
- **User Authentication** — secure login system per company tenant
- **Input Validation** — all user inputs are validated server-side
- **Activity Logging** — all system actions are recorded per company
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
│   ├── TenantController.cs
│   └── AccountController.cs
│
├── Models/                       # Data models
│   ├── KPI.cs
│   ├── Report.cs
│   ├── Tenant.cs
│   └── User.cs
│
├── Views/                        # Razor Pages views
│   ├── Dashboard/
│   ├── Reporting/
│   ├── Analytics/
│   ├── DataVisualization/
│   ├── DecisionSupport/
│   └── Tenant/
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