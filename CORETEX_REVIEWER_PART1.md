# CORETEX EDSS — COMPLETE SYSTEM REVIEWER

> **What is Coretex?** — Coretex is an **Executive Decision Support System (EDSS)** for a computer/electronics retail company. It helps the CEO, Finance Officers, Cashiers, and Admins run the business using real data, live APIs, and smart analytics. Built with **ASP.NET Core MVC + SQL Server**.

---

## PART 1: THE APIs (External Data Sources)

### Q: What APIs does the system use?

The system uses **4 external APIs** and **2 email services**:

| # | API Name | What It Does | What Data It Gets | Where It's Used |
|---|----------|-------------|-------------------|-----------------|
| 1 | **SerpApi (Google Trends)** | Gets real-time market interest data | Search popularity scores (0-100) for products like Laptops, Smartphones, Tablets in the Philippines | CEO > Predictive Intelligence page |
| 2 | **NewsAPI** | Gets live news articles | Headlines, descriptions, images, URLs from 20+ news sources | CEO > Live News Feed page |
| 3 | **ExchangeRate-API** | Converts foreign currency to PHP | Live exchange rates (e.g., USD to PHP) | Finance > when adding expenses in foreign currency |
| 4 | **IP Geolocation API** | Detects the user's physical location from their IP address | City, Country, ISP | Login page (security tracking), Activity Logs |
| 5 | **SendGrid** | Sends emails (primary) | N/A — sends outgoing emails | OTP codes, Alerts, Reports |
| 6 | **Gmail SMTP** | Sends emails (backup) | N/A — sends outgoing emails | Fallback if SendGrid fails |

---

### Q: How does each API fetch data?

**All APIs use `HttpClient`** — the standard way in C# to make web requests. Here's how each one works in simple English:

#### 1. SerpApi (Google Trends) — `TrendService.cs`
- **How:** The system sends a GET request to `https://serpapi.com/search.json` with the query "Laptops, Smartphones, Tablets" and region "PH" (Philippines).
- **What comes back:** A JSON object with `interest_over_time` data — basically a score from 0-100 showing how popular each product is in Google searches.
- **Fallback:** If the API fails, the system generates realistic fake data so the page still works.
- **File location:** `Services/TrendService.cs`

#### 2. NewsAPI — `NewsService.cs`
- **How:** The system sends a GET request to `https://newsapi.org/v2/everything` with category-based search queries (e.g., "corporate+strategy+startup+finance" for Business category).
- **What comes back:** A JSON array of news articles with title, description, image URL, source name, and publish date.
- **Categories available:** Economy, Technology, Business, Pricing
- **File location:** `Services/NewsService.cs`

#### 3. ExchangeRate-API — `ExchangeRateService.cs`
- **How:** The system sends a GET request to `https://v6.exchangerate-api.com/v6/{API_KEY}/pair/{FROM}/PHP/{AMOUNT}`.
- **What comes back:** The converted amount in PHP (Philippine Pesos).
- **Example:** If you enter an expense of $100 USD, the system calls the API and converts it to ~₱5,600 PHP automatically before saving.
- **Fallback:** If the API fails, it just saves the original amount.
- **File location:** `Services/ExchangeRateService.cs`

#### 4. IP Geolocation — `GeolocationService.cs`
- **How:** The system grabs the user's IP address, then sends a GET request to `https://api.ipgeolocation.io/ipgeo` with that IP.
- **Special trick:** If the user is on localhost (::1 or 127.0.0.1), the system first calls `https://api.ipify.org` to get the real public IP.
- **What comes back:** City name, Country name, and ISP (Internet Service Provider).
- **File location:** `Services/GeolocationService.cs`

---

### Q: Which APIs are "data-based" (pull data into the system)?

| API | Data-Based? | Why? |
|-----|------------|------|
| SerpApi | YES | Pulls real Google Trends data into the CEO dashboard for market analysis |
| NewsAPI | YES | Pulls real news articles for the CEO to read and react to |
| ExchangeRate-API | YES | Pulls real exchange rates to convert expense amounts |
| IP Geolocation | YES | Pulls real location data for every login and action |

All 4 APIs are data-based — they all pull live external data into the system.

---

### Q: Why did you choose THESE specific APIs?

| API | Why This One? |
|-----|--------------|
| **SerpApi** | Because we need to know what products are trending in the Philippines. Google Trends is the most reliable source for real consumer search interest. This helps the CEO decide what to stock more of. |
| **NewsAPI** | Because the CEO needs to know what's happening in the tech/business world. News affects pricing, demand, and strategy. NewsAPI gives us 20+ sources in one call. |
| **ExchangeRate-API** | Because a tech retail business may buy inventory from international suppliers. Expenses could be in USD, JPY, etc. We need accurate conversion to PHP for proper financial records. |
| **IP Geolocation** | Because security is critical. If someone logs in from an unusual location (e.g., a different country), the system needs to detect and alert the user. This prevents unauthorized access. |

---

### Q: Where is each API located in the code?

```
coretex-finalproj/
├── Services/
│   ├── TrendService.cs          ← SerpApi (Google Trends)
│   ├── NewsService.cs           ← NewsAPI
│   ├── ExchangeRateService.cs   ← ExchangeRate-API
│   ├── GeolocationService.cs    ← IP Geolocation API
│   ├── EmailSender.cs           ← SendGrid + Gmail SMTP
│   └── NotificationService.cs   ← SendGrid (for alerts)
├── Controllers/
│   ├── CeoController.cs         ← Calls TrendService & NewsService
│   ├── FinanceController.cs     ← Calls ExchangeRateService
│   └── HomeController.cs        ← Calls GeolocationService (login)
└── appsettings.json             ← All API keys stored here
```

---

## PART 2: THE 7 MODULES — Where, How, When

### Module 1: Expense Management Module

**What it is:** The Finance team uses this to record all business expenses (COGS, Rent, Salaries, Utilities).

**Where it is:**
- Controller: `FinanceController.cs` → `CreateExpense()`, `Dashboard()`
- Views: `Finance/ExpensesCogs.cshtml`, `ExpensesRent.cshtml`, `ExpensesSalaries.cshtml`, `ExpensesUtilities.cshtml`
- Model: `Models/Expense.cs`

**How it works:**
1. The Finance Officer opens the Expense page (e.g., `/finance/expenses/cogs`)
2. They fill in the amount, description, and date
3. They can optionally pick a foreign currency (USD, EUR, JPY, etc.)
4. If foreign currency is selected, the system calls **ExchangeRate-API** to convert it to PHP
5. The expense is saved to the SQL database under that branch
6. Every action is logged in the Activity Log

**When it's applied:** Whenever the Finance Officer needs to record a business expense.

---

### Module 2: Branch Data Confirmation Module

**What it is:** At the end of each month, the Finance Officer "submits" the month — locking all sales and expenses into an official record.

**Where it is:**
- Controller: `FinanceController.cs` → `SubmitMonth()`, `GetFinanceSnapshot()`, `GetSubmissionHistory()`
- View: `Finance/Submissions.cshtml`, `Finance/Review.cshtml`
- Model: `Models/BranchSubmission.cs`

**How it works:**
1. Finance Officer clicks "Submit Month" for a specific month (e.g., May 2026)
2. The system calculates total Sales and total Expenses from the database for that month
3. It breaks down expenses into COGS, Rent, Salaries, Utilities
4. Creates a `BranchSubmission` record with all these totals
5. Once submitted, that month is "locked" — no more changes allowed
6. The CEO can then see the confirmed data in their dashboard

**When it's applied:** End of every month, when the branch is done recording all transactions.

---

### Module 3: KPI Dashboard Module

**What it is:** The CEO's main dashboard showing Key Performance Indicators — Revenue, Expenses, Profit, Margin, and Risk Score.

**Where it is:**
- Controller: `CeoController.cs` → `Dashboard()`, `KpiProfitMargin()`, `KpiExpenseRatio()`, `KpiRiskScore()`
- Views: `Ceo/Dashboard.cshtml`, `KpiProfitMargin.cshtml`, `KpiExpenseRatio.cshtml`, `KpiRiskScore.cshtml`
- Service: `Services/AnalyticsService.cs` → `GetDashboardSnapshotAsync()`

**How it works:**
1. When the CEO opens the dashboard, the system calls `AnalyticsService`
2. The service checks if there are official **BranchSubmissions** for the selected period
3. If yes → uses the confirmed numbers
4. If no → falls back to raw Sales and Expenses data from the database
5. Calculates: Revenue, Expenses, Profit = Revenue - Expenses, Margin = (Profit/Revenue) × 100
6. Risk Level: "Healthy" if margin > 15%, "Warning" if below, "Critical" if negative

**When it's applied:** Every time the CEO opens their dashboard or any KPI page.

---

### Module 4: Predictive Trend Analysis Module

**What it is:** Uses historical sales data + Google Trends data to predict future revenue.

**Where it is:**
- Controller: `CeoController.cs` → `AnalyticsPredictive()`, `AnalyticsForecast()`
- View: `Ceo/AnalyticsPredictive.cshtml`, `Ceo/AnalyticsForecast.cshtml`
- Services: `AnalyticsService.cs` → `GetSalesForecastAsync()`, `TrendService.cs`

**How the predictive feature works (step by step):**

1. **Step 1 — Get Historical Data:** The system pulls the last 3 months of sales from the database
2. **Step 2 — Calculate Daily Average:** Total sales ÷ number of days = daily average revenue
3. **Step 3 — Project Forward:** Daily average × 30 = predicted revenue for next month
4. **Step 4 — Get Market Trends:** Calls SerpApi to get Google Trends scores for Laptops, Smartphones, Tablets in PH
5. **Step 5 — Growth Probability:** Formula: `(Market Trend Score × 0.7) + (Sales Momentum × 0.3)` = Growth Probability %
6. **Step 6 — Confidence Band:** The chart shows upper bound (+12%) and lower bound (-12%) around the forecast
7. **Step 7 — Display:** Shows a line chart with solid line (history) and dashed line (forecast)

**When it's applied:** When the CEO opens the Predictive Intelligence page.

---

### Module 5: Data Visualization & Insights Module

**What it is:** Interactive charts and graphs showing financial data visually.

**Where it is:**
- Controller: `CeoController.cs` → `Charts()`, `AnalyticsMom()`
- Views: `Ceo/Charts.cshtml`, `Ceo/AnalyticsMom.cshtml`, `Ceo/AnalyticsExpenseTrend.cshtml`
- Library: **Chart.js** (JavaScript charting library loaded from CDN)

**How it works:**
1. The backend sends monthly data (revenue, expenses, profit per month) as JSON
2. The frontend JavaScript uses **Chart.js** to render line charts, bar charts, and pie charts
3. Charts include: Revenue vs Expenses over time, Expense breakdown by category (pie), Branch performance comparison (bar)
4. "Month-over-Month" (MoM) analysis shows percentage change from one month to the next

**When it's applied:** CEO > Charts page, CEO > Month-over-Month page.

---

### Module 6: Decision Support Analytics Module

**What it is:** The system doesn't just show data — it recommends ACTIONS the CEO can take.

**Where it is:**
- Controller: `CeoController.cs` → `AutonomousPriceAdjustment()`, `AutonomousStockRebalance()`
- View: `Ceo/AnalyticsPredictive.cshtml` → "Strategic Action Center" section
- Admin: `AdminController.cs` → `SaveKpiThreshold()`, `SetGoal()`

**How it works — 3 autonomous actions:**

**Action 1: Price Mitigation (Laptop Cost Volatility)**
- The system warns: "Predicted 15% increase in procurement costs"
- CEO clicks "Execute Mitigation"
- System automatically increases all Laptop prices by 10% in the database
- CEO can click "Stop Strategy" to revert prices back

**Action 2: Stock Rebalance**
- The system detects excess inventory in one branch (e.g., Sandawa has too many Components)
- CEO clicks "Review Transfer"
- System automatically moves half the excess stock to another branch
- Creates the product in the target branch if it doesn't exist

**Action 3: KPI Thresholds & Breach Detection**
- CEO sets minimum profit margin (e.g., 15%), max expense ratio, etc.
- If actual performance falls below these thresholds → system creates a "Strategic Breach" notification
- The system can send email alerts to the CEO automatically

**When it's applied:** CEO > Predictive Intelligence page (Action Center), Admin > KPI Thresholds.

---

### Module 7: Executive Reporting Module

**What it is:** Generate, save, email, and export business reports.

**Where it is:**
- Controllers: `CeoController.cs` → `ReportsGenerate()`, `SaveReport()`, `GetReports()`, `DeleteReport()`
- Controller: `ReportController.cs` → `GenerateExecutiveReport()`, `ExportReportCsv()`, `TriggerAlertEmail()`
- Views: `Ceo/ReportsGenerate.cshtml`, `Ceo/Reports.cshtml`

**How it works:**
1. **Generate Report:** CEO clicks "Generate" → system pulls all financial data → creates HTML report → saves to database
2. **Email Report:** System sends the report via SendGrid to the CEO's email
3. **Export CSV:** CEO clicks "Export CSV" → system generates a ranked branch performance file → downloads as .csv
4. **Report Archive:** All generated reports are stored in the database and can be viewed/deleted later

**When it's applied:** CEO > Executive Reports page, CEO > Dashboard (Export CSV button).
