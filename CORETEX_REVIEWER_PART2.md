# CORETEX EDSS — COMPLETE SYSTEM REVIEWER (PART 2)

## PART 3: STRATEGIES — Why, What, How

The system has **Strategic Governance** — meaning the CEO can activate special business rules that automatically affect how the Cashier POS works. These strategies are stored in the browser's `localStorage` and checked in real-time.

---

### Strategy 1: Margin Protection (Pricing Floor)

**What is it?** When activated, every sale automatically gets a **+5% surcharge** added on top.

**Why is it there?** To protect profit margins. If the CEO sees that costs are rising (from API trend data), they can activate this to make sure the company doesn't lose money on each sale.

**How it works:**
1. CEO goes to **Predictive Intelligence** page
2. Clicks **"Apply Strategy"** button
3. This sets `localStorage.setItem('coretex_pricing_floor', 'true')`
4. On the **Cashier POS** page, every 1 second, the system checks localStorage
5. If `coretex_pricing_floor` is `true`:
   - A big **indigo banner** appears: "CEO ORDER: MANDATORY PRICING FLOOR"
   - The cart total automatically adds +5% to every transaction
   - The surcharge shows as "Margin Protection (CEO)" in the cart
   - When the sale is submitted, the +5% is included in the actual Amount sent to the database
   - The `StrategyApplied` field on the Sale record says "Margin Protection (CEO)"

**Where in the code:**
- Activation: `Views/Ceo/AnalyticsPredictive.cshtml` → `applyGlobalStrategy()` function
- Enforcement: `Views/Cashier/Pos.cshtml` → `renderCart()` function (lines ~464-475)
- Database commit: `Views/Cashier/Pos.cshtml` → `processCheckout()` function (lines ~526-529)

---

### Strategy 2: Marketing Rebate (Marketing Blitz)

**What is it?** When activated, every sale automatically gets a **-10% discount** applied.

**Why is it there?** To boost sales volume during slow periods. If the CEO sees low demand from trend data, they can launch a promotion to attract more customers.

**How it works:**
1. CEO activates it (sets `localStorage.setItem('coretex_marketing_blitz', 'true')`)
2. On the Cashier POS, a **green banner** appears: "MARKETING BLITZ: PROMOTION LIVE"
3. The cart total automatically subtracts 10% from every transaction
4. Shows as "Marketing Rebate (CEO)" in the cart
5. The discount is included in the actual database amount

**Where in the code:**
- Enforcement: `Views/Cashier/Pos.cshtml` → `renderCart()` function (lines ~477-488)

---

### Strategy 3: Capital Freeze (Crisis Protocol)

**What is it?** When activated, **high-value sales (>₱50,000) are blocked** at the POS.

**Why is it there?** For emergencies. If the company is in a cash crisis, the CEO can freeze big transactions to preserve capital.

**How it works:**
1. CEO activates it (sets `localStorage.setItem('coretex_capital_freeze', 'true')`)
2. On the Cashier POS, a **red banner** appears: "CRISIS PROTOCOL: CAPITAL FREEZE"
3. If a cashier tries to add a product priced over ₱50,000, a modal pops up: "CAPITAL FREEZE ACTIVE — SECURITY BLOCK"
4. The item is NOT added to the cart

**Where in the code:**
- Enforcement: `Views/Cashier/Pos.cshtml` → `addToCart()` function (lines ~397-401)

---

### Strategy 4: Autonomous Price Adjustment

**What is it?** CEO can automatically increase/decrease prices for an entire product category.

**Why is it there?** If market data (from SerpApi) shows that laptop costs will rise 15%, the CEO can proactively raise prices to protect margins.

**How it works:**
1. CEO clicks "Execute Mitigation" on the Predictive page
2. Frontend sends POST to `/ceo/AutonomousPriceAdjustment` with `{ Category: "Laptops", Percentage: 10 }`
3. Backend finds ALL products in the "Laptops" category
4. Multiplies each product's price by `(1 + 10/100)` = 1.10 (10% increase)
5. Saves to database
6. CEO can click "Stop Strategy" to revert (applies -9.09% to undo the +10%)

**Where in the code:**
- Backend: `Controllers/CeoController.cs` → `AutonomousPriceAdjustment()` method (line 331)
- Frontend: `Views/Ceo/AnalyticsPredictive.cshtml` → `applyMitigationStrategy()` function

---

### Strategy 5: Autonomous Stock Rebalance

**What is it?** CEO can automatically transfer excess stock from one branch to another.

**Why is it there?** If one branch has too much inventory (>20 units) and another branch is low, the system can balance them out automatically.

**How it works:**
1. CEO clicks "Review Transfer" on the Predictive page
2. Frontend sends POST to `/ceo/AutonomousStockRebalance` with `{ Category: "Components", SourceBranch: "Sandawa" }`
3. Backend finds the source branch, finds products with stock > 20
4. Moves HALF the stock to another active branch
5. If the product doesn't exist in the target branch, it creates it
6. Saves to database and logs the action

**Where in the code:**
- Backend: `Controllers/CeoController.cs` → `AutonomousStockRebalance()` method (line 351)

---

### Strategy 6: Self-Healing Monitor

**What is it?** The system automatically deactivates a strategy when the market data shows the problem is resolved.

**Why is it there?** So the CEO doesn't need to manually watch and turn off strategies. The system is smart enough to fix itself.

**How it works:**
1. Every 5 seconds, the Predictive page checks the latest market trend score
2. If the price mitigation strategy is active AND the trend score drops below 50 (meaning demand dropped, less volatility)
3. The system automatically shows: "AUTONOMOUS RECOVERY: Market data shows volatility has stabilized"
4. The page reloads, and the strategy is deactivated

**Where in the code:**
- `Views/Ceo/AnalyticsPredictive.cshtml` → `setInterval()` at the bottom (lines ~467-476)

---

## PART 4: SECURITY — Everything

### Q: What security measures does the system have?

| # | Security Feature | How It Works | Where in Code |
|---|-----------------|--------------|---------------|
| 1 | **Password Complexity (NIST)** | Minimum 12 characters, requires uppercase, lowercase, digit, and special character | `Program.cs` lines 48-53 |
| 2 | **Account Lockout** | After 5 failed login attempts, account is locked for 15 minutes | `Program.cs` lines 43-45 |
| 3 | **Two-Factor Auth (2FA/OTP)** | After password, user gets a one-time code via email that must be entered | `HomeController.cs` → `VerifyOTP()` |
| 4 | **Session Timeout** | Auto-logout after 20 minutes of inactivity | `Program.cs` line 66 |
| 5 | **IP Rate Limiting** | Max 5 login attempts per IP per minute. Uses in-memory cache | `SecurityService.cs` → `IsRateLimited()` |
| 6 | **Geolocation Alert** | If you login from a different city than last time, you get a security email | `HomeController.cs` → `ProcessSecurityAlerts()` |
| 7 | **Content Security Policy (CSP)** | HTTP header that only allows scripts/styles from trusted CDNs | `Program.cs` lines 97-110 |
| 8 | **X-Frame-Options: DENY** | Prevents clickjacking — no one can embed the site in an iframe | `Program.cs` line 107 |
| 9 | **X-Content-Type-Options: nosniff** | Prevents MIME-type attacks | `Program.cs` line 108 |
| 10 | **HttpOnly Cookies** | Login cookies can't be stolen by JavaScript (XSS protection) | `Program.cs` line 68 |
| 11 | **Data Encryption** | Sensitive data encrypted using ASP.NET Data Protection API | `SecurityService.cs` → `Encrypt()` / `Decrypt()` |
| 12 | **Role-Based Access (RBAC)** | Each page is locked to specific roles using `[Authorize(Roles = "...")]` | Every controller |
| 13 | **Branch Isolation** | Users can only see data from their own branch (unless Admin/CEO) | `FinanceController.cs` line 37-40 |
| 14 | **Audit Logging** | EVERY action is recorded: who, what, when, from where (IP + location) | `AuditLoggingService.cs` |
| 15 | **Branch Node Integrity** | If a branch is archived/inactive, its users CANNOT login | `HomeController.cs` lines 102-113 |
| 16 | **CEO Protection** | Non-CEO admins cannot modify/delete/demote the CEO account | `AdminController.cs` lines 359-363 |
| 17 | **Soft Delete** | Users are never truly deleted — they're deactivated (archived) | `AdminController.cs` line 418 |

---

## PART 5: CEO DASHBOARD — Every "Pill" (Card) Explained

The CEO Dashboard at `/ceo/dashboard` has **5 KPI cards** at the top and a **navigation pill bar** that links to every CEO sub-page. Here's each one:

### The 5 Top KPI Cards

| Card | What It Shows | Why It's There | What Happens When Clicked |
|------|--------------|----------------|--------------------------|
| **Revenue** | Total sales in PHP (e.g., ₱1,234,567) | The CEO needs to know total income at a glance | Goes to `/ceo/analytics/forecast` (Revenue Forecast page) |
| **Expenses** | Total expenses in PHP | The CEO needs to know total spending | Goes to `/ceo/kpi/expense-ratio` (Expense Analysis page) |
| **Profit** | Revenue minus Expenses | The most important number — is the company making money? | Goes to `/ceo/analytics/mom` (Month-over-Month page) |
| **Margin** | Profit ÷ Revenue × 100 (percentage) | Shows efficiency — a high margin means the company keeps more of each peso earned | Goes to `/ceo/kpi/profit-margin` (Margin Analysis page) |
| **Risk Score** | "Healthy" (green), "Warning" (yellow), or "Critical" (red) | Instant visual of business health. Green = profit margin above 15%. Warning = below 15%. Critical = losing money | Goes to `/ceo/kpi/risk-score` (Risk Assessment page) |

### The CEO Navigation Pills (Sub-Pages)

| Pill/Page | Route | Purpose |
|-----------|-------|---------|
| **Dashboard** | `/ceo/dashboard` | Main command center with KPIs, financial chart, branch ranking, and system alerts |
| **Profit Margin** | `/ceo/kpi/profit-margin` | Deep dive into profit margins per branch with threshold lines and comparison table |
| **Expense Ratio** | `/ceo/kpi/expense-ratio` | Pie chart of expense categories (COGS, Rent, Salaries, Utilities) and expense-to-revenue ratio |
| **Revenue Forecast** | `/ceo/analytics/forecast` | Predicts next month's revenue using historical average. Shows confidence intervals |
| **Expense Trend** | `/ceo/analytics/expense-trend` | Visualizes how expenses are distributed across categories |
| **Risk Score** | `/ceo/kpi/risk-score` | Full risk assessment based on profit, margin, and KPI thresholds. Shows recommended actions |
| **Health Summary** | `/ceo/analytics/health-summary` | Overall business health report with traffic-light indicators |
| **Branch Compare** | `/ceo/branches/compare` | Side-by-side ranking of all branches by revenue, expenses, and profit |
| **Month-over-Month** | `/ceo/analytics/mom` | Shows percentage change in revenue/expenses from one month to the next |
| **Interactive Charts** | `/ceo/charts` | Plotly.js-powered interactive charts — zoom, hover, pan on financial data |
| **Predictive Intelligence** | `/ceo/analytics/predictive` | Forecast engine + Google Trends + Strategic Action Center (price adjust, stock rebalance) |
| **Live News Feed** | `/ceo/news` | Real-time news from NewsAPI + ability to broadcast executive strategy to all employees |
| **Executive Reports** | `/ceo/reports/generate` | Generate, save, email, and export business reports |
| **Audit Trail** | `/ceo/audit-trail` | View all system activity logs — who did what, when, from where |

---

## PART 6: USER ROLES — Who Does What

| Role | Dashboard URL | What They Can Do |
|------|--------------|------------------|
| **CEO** | `/ceo/dashboard` | View all analytics, KPIs, predictions, news. Activate strategies. Set goals & KPI thresholds. Generate reports. View audit trail. Cannot be modified by non-CEO users. |
| **ADMIN** | `/admin` | Manage branches (create, edit, archive). Manage users (create, edit roles, reset passwords). View activity logs. Set report schedules. |
| **BRANCH_ADMIN** | `/admin` | Same as Admin but limited to their own branch only. Can see branch-specific stats. |
| **FINANCE** | `/finance/dashboard` | Record expenses (COGS, Rent, Salaries, Utilities). Submit monthly financial reports. View expense history. Currency conversion. |
| **CASHIER** | `/cashier/pos` | Process sales at the POS terminal. View daily summaries. Check stock health. Request restocks. View personal activity log. |

---

## PART 7: DATA FLOW — How Everything Connects

```
CASHIER (POS)                    FINANCE
   │                                │
   │ Creates Sales                  │ Creates Expenses
   │ (Product, Qty, Price)          │ (Amount, Category, Currency)
   │                                │
   ▼                                ▼
┌─────────────────────────────────────────────┐
│              SQL SERVER DATABASE             │
│  Sales Table │ Expenses Table │ Products     │
│  BranchSubmissions │ ActivityLogs            │
└─────────────┬───────────────────────────────┘
              │
              │  AnalyticsService reads data
              │
              ▼
┌─────────────────────────────────────────────┐
│           CEO DASHBOARD                      │
│  KPIs ← AnalyticsService.GetSnapshot()      │
│  Charts ← AnalyticsService.GetMonthlyPnL()  │
│  Forecast ← AnalyticsService.GetForecast()  │
│  Trends ← TrendService (SerpApi)            │
│  News ← NewsService (NewsAPI)               │
│                                              │
│  CEO ACTIVATES STRATEGY                      │
│  ↓ localStorage                              │
│  ↓ Cashier POS reads it in real-time         │
│  ↓ Prices/discounts/blocks applied           │
└──────────────────────────────────────────────┘
```

---

## PART 8: TECH STACK SUMMARY

| Component | Technology |
|-----------|-----------|
| Backend | ASP.NET Core 8 (MVC Pattern) |
| Language | C# |
| Database | Microsoft SQL Server |
| ORM | Entity Framework Core |
| Authentication | ASP.NET Identity + 2FA |
| Frontend | Razor Views + TailwindCSS + Chart.js + Plotly.js |
| Email | SendGrid API + Gmail SMTP (MailKit) |
| External APIs | SerpApi, NewsAPI, ExchangeRate-API, IP Geolocation |
| Hosting | DatabaseASP.net (SQL), IIS/Kestrel (Web) |
| Security | CSP Headers, RBAC, Rate Limiting, Data Protection API |

---

## PART 9: QUICK FIRE Q&A

**Q: What design pattern does the system use?**
A: MVC (Model-View-Controller). Models are in `/Models`, Views in `/Views`, Controllers in `/Controllers`. Services handle business logic.

**Q: How does data get from the Cashier to the CEO?**
A: Cashier creates Sales → saved to SQL → AnalyticsService reads Sales table → CEO dashboard displays it.

**Q: What happens if an API fails?**
A: Every API has a fallback. SerpApi returns fake data. ExchangeRate returns original amount. Geolocation returns "Unknown". NewsAPI shows an error message. The system never crashes.

**Q: How does the submission system prevent double-counting?**
A: The AnalyticsService checks if a BranchSubmission exists for a month. If yes, it uses the submission data. If no, it uses raw Sales/Expenses. It never adds both.

**Q: What is "Hybrid Data" in the analytics?**
A: For all-time totals, the system adds: (official submissions) + (live data for months that DON'T have submissions yet). This ensures complete but accurate totals.

**Q: How does the CEO broadcast reach the Cashier?**
A: The CEO types a message on the News page → saved to localStorage → the POS page checks localStorage every 1 second → if a message exists, it shows as a banner.

**Q: Can a branch admin see other branches?**
A: No. Branch isolation is enforced. Branch Admins only see their own branch's users, data, and logs.

**Q: How does the audit log know the user's location?**
A: Every log entry calls `GeolocationService.GetLocationAsync()` which calls the IP Geolocation API with the user's IP address and gets back City + Country.

---

> **TIP FOR DEFENSE:** When asked "how does X work?", always explain: (1) what triggers it, (2) what controller/service handles it, (3) what data it reads/writes, (4) what the user sees. That covers everything.
