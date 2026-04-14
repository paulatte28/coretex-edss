(function () {
    'use strict';

    const KEYS = {
        thresholdAlerts: 'coretex_threshold_alerts',
        activities: 'coretex_activities',
        reports: 'coretex_ceo_reports',
        newsApiKey: 'coretex_news_api_key',
        monthlyData: 'coretex_monthly_data',
        monthlySummaries: 'coretex_monthly_summaries',
        submissions: 'coretex_submissions',
        expenses: 'coretex_finance_expenses'
    };

    const NEWS_FALLBACK = {
        business: [
            { title: 'Retail spending expands as consumer confidence improves', source: 'Market Wire', publishedAt: '2026-04-11', url: 'https://example.com/business-1' },
            { title: 'SME lending volume climbs in Q1 amid lower default rates', source: 'Finance Journal', publishedAt: '2026-04-10', url: 'https://example.com/business-2' },
            { title: 'Local logistics costs cool, improving distributor margins', source: 'Commerce Daily', publishedAt: '2026-04-09', url: 'https://example.com/business-3' }
        ],
        technology: [
            { title: 'AI demand forecasting tools gain traction in retail chains', source: 'Tech Ledger', publishedAt: '2026-04-11', url: 'https://example.com/tech-1' },
            { title: 'Cloud ERP adoption rises among regional distributors', source: 'Systems Report', publishedAt: '2026-04-10', url: 'https://example.com/tech-2' },
            { title: 'POS analytics platforms add real-time anomaly alerts', source: 'Digital Ops', publishedAt: '2026-04-09', url: 'https://example.com/tech-3' }
        ],
        economy: [
            { title: 'Inflation eases slightly as food supply conditions improve', source: 'Economic Monitor', publishedAt: '2026-04-11', url: 'https://example.com/economy-1' },
            { title: 'Peso remains stable against major currencies this week', source: 'Macro Bulletin', publishedAt: '2026-04-10', url: 'https://example.com/economy-2' },
            { title: 'Industrial output growth supports optimistic Q2 outlook', source: 'National Business Post', publishedAt: '2026-04-09', url: 'https://example.com/economy-3' }
        ]
    };

    const NAV_ITEMS = [
        { key: 'dashboard', label: 'Dashboard', href: '/ceo/dashboard' },
        { key: 'profit-margin', label: 'Profit Margin', href: '/ceo/kpi/profit-margin' },
        { key: 'expense-ratio', label: 'Expense Ratio', href: '/ceo/kpi/expense-ratio' },
        { key: 'forecast', label: 'Revenue Forecast', href: '/ceo/analytics/forecast' },
        { key: 'expense-trend', label: 'Expense Trend', href: '/ceo/analytics/expense-trend' },
        { key: 'risk-score', label: 'Risk Score', href: '/ceo/kpi/risk-score' },
        { key: 'health-summary', label: 'Health Summary', href: '/ceo/analytics/health-summary' },
        { key: 'branches-compare', label: 'Branches', href: '/ceo/branches/compare' },
        { key: 'mom', label: 'MoM', href: '/ceo/analytics/mom' },
        { key: 'charts', label: 'Interactive Charts', href: '/ceo/charts' },
        { key: 'predictive', label: 'Predictive', href: '/ceo/analytics/predictive' },
        { key: 'news', label: 'News Feed', href: '/ceo/news' },
        { key: 'report-generate', label: 'Generate Report', href: '/ceo/reports/generate' },
        { key: 'reports', label: 'Reports', href: '/ceo/reports' }
    ];

    const charts = {};

    function readArray(key) {
        try {
            const value = JSON.parse(localStorage.getItem(key) || '[]');
            return Array.isArray(value) ? value : [];
        } catch (_error) {
            return [];
        }
    }

    function writeArray(key, value) {
        localStorage.setItem(key, JSON.stringify(value));
    }

    function money(value) {
        return `\u20B1${Number(value || 0).toLocaleString('en-PH', {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2
        })}`;
    }

    function pct(value) {
        return `${Number(value || 0).toFixed(1)}%`;
    }

    function toNumber(value) {
        const parsed = Number(value);
        return Number.isFinite(parsed) ? parsed : 0;
    }

    function monthKeyFromDate(date) {
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, '0');
        return `${year}-${month}`;
    }

    function monthToDate(monthValue) {
        const text = String(monthValue || '').trim();
        if (/^\d{4}-\d{2}$/.test(text)) {
            return new Date(`${text}-01T00:00:00`);
        }

        return new Date(`${text} 01`);
    }

    function monthKeyToLabel(monthKey) {
        const date = monthToDate(monthKey);
        if (Number.isNaN(date.getTime())) return String(monthKey || 'Unknown');

        return date.toLocaleDateString('en-US', {
            month: 'short',
            year: 'numeric'
        });
    }

    function parseMonthKey(monthValue, yearValue) {
        const text = String(monthValue || '').trim();

        if (/^\d{4}-\d{2}$/.test(text)) {
            return text;
        }

        const numericYear = Number(yearValue);
        const numericMonth = Number(monthValue);
        if (Number.isInteger(numericYear) && Number.isInteger(numericMonth) && numericMonth >= 1 && numericMonth <= 12) {
            return `${numericYear}-${String(numericMonth).padStart(2, '0')}`;
        }

        const composed = yearValue ? `${text} 1, ${yearValue}` : `${text} 1`;
        const parsed = new Date(composed);
        if (Number.isNaN(parsed.getTime())) {
            return null;
        }

        return monthKeyFromDate(parsed);
    }

    function addMonths(monthKey, offset) {
        const date = monthToDate(monthKey);
        if (Number.isNaN(date.getTime())) {
            return monthKeyFromDate(new Date());
        }

        const moved = new Date(date.getFullYear(), date.getMonth() + offset, 1);
        return monthKeyFromDate(moved);
    }

    function formatChange(current, previous) {
        const curr = toNumber(current);
        const prev = toNumber(previous);

        if (prev <= 0) {
            if (curr <= 0) return '+0.0%';
            return '+100.0%';
        }

        const delta = ((curr - prev) / prev) * 100;
        const sign = delta >= 0 ? '+' : '-';
        return `${sign}${Math.abs(delta).toFixed(1)}%`;
    }

    function getMonthBranchEntries() {
        const entries = new Map();

        function ensureEntry(monthKey, branchId, branchName) {
            if (!monthKey) return null;

            const normalizedBranchId = String(branchId || 'branch_default');
            const normalizedBranchName = String(branchName || 'Main Branch');
            const key = `${monthKey}|${normalizedBranchId}`;

            if (!entries.has(key)) {
                entries.set(key, {
                    monthKey,
                    month: monthKeyToLabel(monthKey),
                    branchId: normalizedBranchId,
                    branchName: normalizedBranchName,
                    revenue: 0,
                    expenses: 0,
                    cogs: 0,
                    rent: 0,
                    salaries: 0,
                    utilities: 0
                });
            }

            return entries.get(key);
        }

        readArray(KEYS.submissions).forEach(item => {
            const monthKey = parseMonthKey(item.month || item.monthKey, item.year);
            const entry = ensureEntry(monthKey, item.branchId, item.branchName);
            if (!entry) return;

            entry.revenue = Math.max(entry.revenue, toNumber(item.totalSales));
            entry.expenses = Math.max(entry.expenses, toNumber(item.totalExpenses));

            if (item.summarySnapshot && item.summarySnapshot.expenses) {
                entry.cogs = Math.max(entry.cogs, toNumber(item.summarySnapshot.expenses.cogs));
                entry.rent = Math.max(entry.rent, toNumber(item.summarySnapshot.expenses.rent));
                entry.salaries = Math.max(entry.salaries, toNumber(item.summarySnapshot.expenses.salaries));
                entry.utilities = Math.max(entry.utilities, toNumber(item.summarySnapshot.expenses.utilities));
            }
        });

        readArray(KEYS.monthlySummaries).forEach(item => {
            const monthKey = parseMonthKey(item.month || item.monthKey, item.year);
            const entry = ensureEntry(monthKey, item.branchId, item.branchName);
            if (!entry) return;

            entry.revenue = Math.max(entry.revenue, toNumber(item.totalSales));
            entry.expenses = Math.max(entry.expenses, toNumber(item.totalExpenses));
            entry.cogs = Math.max(entry.cogs, toNumber(item.totalImports));
        });

        readArray(KEYS.monthlyData).forEach(item => {
            const monthKey = parseMonthKey(item.monthKey || item.month, item.year);
            const entry = ensureEntry(monthKey, item.branchId, item.branchName);
            if (!entry) return;

            entry.revenue = Math.max(entry.revenue, toNumber(item.sales ?? item.revenue ?? item.totalSales));
            entry.expenses = Math.max(entry.expenses, toNumber(item.expenses ?? item.totalExpenses));
            entry.cogs = Math.max(entry.cogs, toNumber(item.cogs ?? item.importedPHP ?? item.importedCostPHP));
            entry.rent = Math.max(entry.rent, toNumber(item.rent));
            entry.salaries = Math.max(entry.salaries, toNumber(item.salaries));
            entry.utilities = Math.max(entry.utilities, toNumber(item.utilities));
        });

        readArray(KEYS.expenses).forEach(item => {
            if (item && item.deleted) return;

            const monthKey = parseMonthKey(item.monthKey || item.month, item.year);
            const entry = ensureEntry(monthKey, item.branchId, item.branchName);
            if (!entry) return;

            const amount = toNumber(item.amountPHP ?? item.amount ?? item.value);
            const category = String(item.category || '').toLowerCase();

            if (category === 'cogs') entry.cogs += amount;
            if (category === 'rent') entry.rent += amount;
            if (category === 'salaries') entry.salaries += amount;
            if (category === 'utilities') entry.utilities += amount;
        });

        entries.forEach(entry => {
            const categoryTotal = entry.cogs + entry.rent + entry.salaries + entry.utilities;
            if (categoryTotal > 0) {
                entry.expenses = categoryTotal;
            }

            entry.month = monthKeyToLabel(entry.monthKey);
            entry.revenue = Number(entry.revenue.toFixed(2));
            entry.expenses = Number(entry.expenses.toFixed(2));
            entry.cogs = Number(entry.cogs.toFixed(2));
            entry.rent = Number(entry.rent.toFixed(2));
            entry.salaries = Number(entry.salaries.toFixed(2));
            entry.utilities = Number(entry.utilities.toFixed(2));
        });

        return [...entries.values()].sort((a, b) => monthToDate(a.monthKey) - monthToDate(b.monthKey));
    }

    function getMonthSeries() {
        const consolidated = new Map();

        getMonthBranchEntries().forEach(item => {
            if (!consolidated.has(item.monthKey)) {
                consolidated.set(item.monthKey, {
                    monthKey: item.monthKey,
                    month: monthKeyToLabel(item.monthKey),
                    revenue: 0,
                    expenses: 0,
                    cogs: 0,
                    rent: 0,
                    salaries: 0,
                    utilities: 0
                });
            }

            const row = consolidated.get(item.monthKey);
            row.revenue += item.revenue;
            row.expenses += item.expenses;
            row.cogs += item.cogs;
            row.rent += item.rent;
            row.salaries += item.salaries;
            row.utilities += item.utilities;
        });

        const series = [...consolidated.values()]
            .sort((a, b) => monthToDate(a.monthKey) - monthToDate(b.monthKey))
            .map(item => ({
                ...item,
                revenue: Number(item.revenue.toFixed(2)),
                expenses: Number(item.expenses.toFixed(2)),
                cogs: Number(item.cogs.toFixed(2)),
                rent: Number(item.rent.toFixed(2)),
                salaries: Number(item.salaries.toFixed(2)),
                utilities: Number(item.utilities.toFixed(2))
            }));

        if (series.length === 0) {
            const monthKey = monthKeyFromDate(new Date());
            return [{
                monthKey,
                month: monthKeyToLabel(monthKey),
                revenue: 0,
                expenses: 0,
                cogs: 0,
                rent: 0,
                salaries: 0,
                utilities: 0
            }];
        }

        return series;
    }

    function getKpiSnapshot() {
        const series = getMonthSeries();
        const current = series[series.length - 1];
        const previous = series.length > 1 ? series[series.length - 2] : current;

        const netProfit = current.revenue - current.expenses;
        const previousProfit = previous.revenue - previous.expenses;
        const profitMargin = current.revenue > 0 ? (netProfit / current.revenue) * 100 : 0;

        return {
            totalRevenue: current.revenue,
            totalExpenses: current.expenses,
            netProfit,
            profitMargin: Number(profitMargin.toFixed(1)),
            revenueChange: formatChange(current.revenue, previous.revenue),
            expensesChange: formatChange(current.expenses, previous.expenses),
            profitChange: formatChange(netProfit, previousProfit),
            risk: getRiskSummary()
        };
    }

    function getProfitMarginTrend() {
        return getMonthSeries().map(item => {
            const profit = item.revenue - item.expenses;
            const margin = item.revenue > 0 ? ((profit / item.revenue) * 100) : 0;
            return {
                month: item.month,
                margin: Number(margin.toFixed(1))
            };
        });
    }

    function getExpenseRatioTrend() {
        return getMonthSeries().map(item => ({
            month: item.month,
            ratio: Number((item.revenue > 0 ? ((item.expenses / item.revenue) * 100) : 0).toFixed(1))
        }));
    }

    function getExpenseBreakdownCurrent() {
        const series = getMonthSeries();
        const current = series[series.length - 1];

        return {
            cogs: current.cogs,
            rent: current.rent,
            salaries: current.salaries,
            utilities: current.utilities
        };
    }

    function getForecastRows() {
        const series = getMonthSeries();
        const recent = series.slice(-4);
        const latest = recent[recent.length - 1];

        let averageGrowth = 0;
        if (recent.length > 1) {
            const growthRates = [];
            for (let i = 1; i < recent.length; i += 1) {
                const previousRevenue = recent[i - 1].revenue;
                const currentRevenue = recent[i].revenue;
                if (previousRevenue > 0) {
                    growthRates.push((currentRevenue - previousRevenue) / previousRevenue);
                }
            }

            if (growthRates.length > 0) {
                averageGrowth = growthRates.reduce((sum, item) => sum + item, 0) / growthRates.length;
            }
        }

        const confidence = [85, 75, 65];
        const baseMonthKey = latest.monthKey || monthKeyFromDate(new Date());
        let projected = latest.revenue;

        const rows = [];
        for (let i = 1; i <= 3; i += 1) {
            projected = Math.max(0, projected * (1 + averageGrowth));
            const monthKey = addMonths(baseMonthKey, i);
            rows.push({
                month: monthKeyToLabel(monthKey),
                projectedRevenue: Number(projected.toFixed(2)),
                confidence: confidence[i - 1]
            });
        }

        return rows;
    }

    function getRiskSummary() {
        const alerts = readArray(KEYS.thresholdAlerts);
        const latest = alerts[0] || null;

        if (latest && latest.severity === 'red') {
            return {
                code: 'RED',
                icon: '🔴',
                headline: 'Critical: KPI Threshold Breach',
                detail: 'Warning: High Expense Ratio'
            };
        }

        if (latest && latest.severity === 'yellow') {
            return {
                code: 'YELLOW',
                icon: '🟡',
                headline: 'Warning: KPI Requires Attention',
                detail: 'Warning: High Expense Ratio'
            };
        }

        return {
            code: 'GREEN',
            icon: '🟢',
            headline: 'Business Health: Healthy',
            detail: 'Business Health: Healthy'
        };
    }

    function getBranchComparisonRows() {
        const entries = getMonthBranchEntries();

        if (entries.length === 0) {
            const latest = getMonthSeries()[getMonthSeries().length - 1];
            const profit = latest.revenue - latest.expenses;
            return [{
                branch: 'All Branches',
                revenue: latest.revenue,
                expenses: latest.expenses,
                profit,
                margin: latest.revenue > 0 ? Number(((profit / latest.revenue) * 100).toFixed(1)) : 0,
                rank: '-'
            }];
        }

        const latestMonthKey = entries[entries.length - 1].monthKey;
        const sameMonth = entries
            .filter(item => item.monthKey === latestMonthKey)
            .map(item => {
                const profit = item.revenue - item.expenses;
                const margin = item.revenue > 0 ? (profit / item.revenue) * 100 : 0;
                return {
                    branch: item.branchName || item.branchId,
                    revenue: item.revenue,
                    expenses: item.expenses,
                    profit,
                    margin: Number(margin.toFixed(1))
                };
            })
            .sort((a, b) => b.profit - a.profit);

        const medals = ['🥇', '🥈', '🥉'];
        return sameMonth.map((row, index) => ({
            ...row,
            rank: medals[index] || `#${index + 1}`
        }));
    }

    function getMomData() {
        const series = getMonthSeries();
        const current = series[series.length - 1];
        const previousRaw = series.length > 1 ? series[series.length - 2] : current;

        const currentProfit = current.revenue - current.expenses;
        const previousProfit = previousRaw.revenue - previousRaw.expenses;

        const safePreviousRevenue = previousRaw.revenue > 0 ? previousRaw.revenue : Math.max(current.revenue, 1);
        const safePreviousExpenses = previousRaw.expenses > 0 ? previousRaw.expenses : Math.max(current.expenses, 1);
        const safePreviousProfit = previousProfit !== 0 ? previousProfit : (currentProfit === 0 ? 1 : currentProfit);

        return {
            previous: {
                label: previousRaw.month,
                revenue: Number(safePreviousRevenue.toFixed(2)),
                expenses: Number(safePreviousExpenses.toFixed(2)),
                profit: Number(safePreviousProfit.toFixed(2))
            },
            current: {
                label: current.month,
                revenue: Number(current.revenue.toFixed(2)),
                expenses: Number(current.expenses.toFixed(2)),
                profit: Number(currentProfit.toFixed(2))
            }
        };
    }

    function getNewsFallback(category) {
        return NEWS_FALLBACK[category] || NEWS_FALLBACK.business;
    }

    function getNotifications() {
        const alerts = readArray(KEYS.thresholdAlerts).slice(0, 20).map(alert => ({
            id: alert.id,
            title: alert.metric,
            message: alert.message,
            timestamp: alert.timestamp,
            route: alert.metric === 'Profit Margin'
                ? '/ceo/kpi/profit-margin'
                : alert.metric === 'Expense Ratio'
                    ? '/ceo/kpi/expense-ratio'
                    : '/ceo/kpi/risk-score'
        }));

        const activities = readArray(KEYS.activities)
            .filter(item => String(item.action || '').toLowerCase().includes('threshold alert'))
            .slice(0, 10)
            .map(item => ({
                id: item.id,
                title: item.action,
                message: item.description,
                timestamp: item.timestamp,
                route: '/ceo/kpi/risk-score'
            }));

        return [...alerts, ...activities]
            .sort((a, b) => new Date(b.timestamp) - new Date(a.timestamp))
            .slice(0, 20);
    }

    function renderNav(activeKey, targetId) {
        const target = document.getElementById(targetId);
        if (!target) return;

        target.innerHTML = NAV_ITEMS.map(item => {
            const active = item.key === activeKey;
            return `
                <a href="${item.href}" class="px-3 py-2 text-[10px] font-black uppercase tracking-widest rounded-lg ${active ? 'bg-slate-900 text-white' : 'bg-slate-100 text-slate-600 hover:bg-slate-200'} transition-colors">
                    ${item.label}
                </a>
            `;
        }).join('');
    }

    function timeAgo(iso) {
        const ms = Date.now() - new Date(iso).getTime();
        const mins = Math.floor(ms / 60000);
        const hours = Math.floor(ms / 3600000);
        const days = Math.floor(ms / 86400000);

        if (mins < 1) return 'just now';
        if (mins < 60) return `${mins}m ago`;
        if (hours < 24) return `${hours}h ago`;
        return `${days}d ago`;
    }

    function mountNotifications(buttonId, badgeId, dropdownId) {
        const button = document.getElementById(buttonId);
        const badge = document.getElementById(badgeId);
        const dropdown = document.getElementById(dropdownId);

        if (!button || !badge || !dropdown) return;

        const notifications = getNotifications();
        badge.textContent = String(notifications.length);
        badge.classList.toggle('hidden', notifications.length === 0);

        dropdown.innerHTML = notifications.length === 0
            ? '<p class="text-xs text-slate-400 italic">No alerts right now.</p>'
            : notifications.map(item => `
                <a href="${item.route}" class="block p-3 rounded-lg hover:bg-slate-50 transition-colors border border-slate-100 mb-2">
                    <p class="text-xs font-black text-slate-900">${escapeHtml(item.title || 'Alert')}</p>
                    <p class="text-xs text-slate-600 mt-1">${escapeHtml(item.message || '')}</p>
                    <p class="text-[10px] text-slate-400 mt-1">${timeAgo(item.timestamp)}</p>
                </a>
            `).join('');

        button.addEventListener('click', function () {
            dropdown.classList.toggle('hidden');
        });

        document.addEventListener('click', function (event) {
            const clickInside = button.contains(event.target) || dropdown.contains(event.target);
            if (!clickInside) {
                dropdown.classList.add('hidden');
            }
        });
    }

    function chartCtx(id) {
        const element = document.getElementById(id);
        return element ? element.getContext('2d') : null;
    }

    function destroyChart(id) {
        if (charts[id]) {
            charts[id].destroy();
            delete charts[id];
        }
    }

    function makeChart(id, config) {
        const ctx = chartCtx(id);
        if (!ctx) return null;

        destroyChart(id);
        charts[id] = new Chart(ctx, config);
        return charts[id];
    }

    function escapeHtml(value) {
        const map = { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#039;' };
        return String(value || '').replace(/[&<>"']/g, char => map[char]);
    }

    async function fetchNews(category) {
        const key = localStorage.getItem(KEYS.newsApiKey);

        if (!key) {
            return {
                source: 'fallback',
                articles: getNewsFallback(category)
            };
        }

        try {
            let endpoint = '';
            if (category === 'economy') {
                endpoint = `https://newsapi.org/v2/everything?q=economy&language=en&pageSize=8&sortBy=publishedAt&apiKey=${encodeURIComponent(key)}`;
            } else {
                endpoint = `https://newsapi.org/v2/top-headlines?category=${encodeURIComponent(category)}&language=en&pageSize=8&apiKey=${encodeURIComponent(key)}`;
            }

            const response = await fetch(endpoint);
            if (!response.ok) throw new Error('News API request failed.');
            const data = await response.json();

            const articles = Array.isArray(data.articles)
                ? data.articles.map(item => ({
                    title: item.title,
                    source: item.source ? item.source.name : 'Unknown Source',
                    publishedAt: item.publishedAt,
                    url: item.url
                }))
                : [];

            if (!articles.length) {
                throw new Error('No articles returned.');
            }

            return {
                source: 'newsapi',
                articles
            };
        } catch (_error) {
            return {
                source: 'fallback',
                articles: getNewsFallback(category)
            };
        }
    }

    function readReports() {
        return readArray(KEYS.reports).sort((a, b) => new Date(b.generatedOn) - new Date(a.generatedOn));
    }

    function writeReports(reports) {
        writeArray(KEYS.reports, reports.slice(0, 200));
    }

    function defaultReportPeriodLabel(periodType, monthValue, startDate, endDate) {
        if (periodType === 'month' && monthValue) {
            const date = new Date(`${monthValue}-01`);
            return date.toLocaleDateString('en-US', { month: 'short', year: 'numeric' });
        }

        if (startDate && endDate) {
            return `${startDate} to ${endDate}`;
        }

        return 'Custom Period';
    }

    function generateReportRecord(payload) {
        const id = `report_${Date.now()}_${Math.random().toString(36).slice(2, 7)}`;
        const generatedOn = new Date().toISOString();
        const periodLabel = defaultReportPeriodLabel(payload.periodType, payload.monthValue, payload.startDate, payload.endDate);

        return {
            id,
            title: 'Executive Summary',
            periodLabel,
            generatedOn,
            periodType: payload.periodType,
            monthValue: payload.monthValue,
            startDate: payload.startDate,
            endDate: payload.endDate,
            sections: payload.sections,
            format: 'PDF'
        };
    }

    function createReportPdfContent(report) {
        const kpi = getKpiSnapshot();
        const lines = [
            'Coretex Executive Report',
            `Report: ${report.title}`,
            `Period: ${report.periodLabel}`,
            `Generated On: ${new Date(report.generatedOn).toLocaleString('en-US')}`,
            '',
            `Total Revenue: ${money(kpi.totalRevenue)}`,
            `Total Expenses: ${money(kpi.totalExpenses)}`,
            `Net Profit: ${money(kpi.netProfit)}`,
            `Profit Margin: ${pct(kpi.profitMargin)}`,
            '',
            `Included Sections: ${(report.sections || []).join(', ') || 'None'}`,
            '',
            'This report was generated from frontend simulation mode.'
        ];

        return lines;
    }

    function downloadReport(reportId) {
        const reports = readReports();
        const report = reports.find(item => item.id === reportId);
        if (!report) return false;

        const lines = createReportPdfContent(report);

        if (window.jspdf && window.jspdf.jsPDF) {
            const doc = new window.jspdf.jsPDF();
            doc.setFontSize(14);
            doc.text(lines, 14, 16);
            doc.save(`coretex-executive-${report.periodLabel.replace(/\s+/g, '-').toLowerCase()}.pdf`);
            return true;
        }

        const blob = new Blob([lines.join('\n')], { type: 'text/plain' });
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `coretex-executive-${report.periodLabel.replace(/\s+/g, '-').toLowerCase()}.txt`;
        link.click();
        URL.revokeObjectURL(url);

        return true;
    }

    function emailReport(reportId) {
        const reports = readReports();
        const report = reports.find(item => item.id === reportId);
        if (!report) return false;

        const activities = readArray(KEYS.activities);
        activities.unshift({
            id: `activity_${Date.now()}`,
            action: 'Executive Report Emailed',
            description: `SendGrid simulation: ${report.title} (${report.periodLabel}) emailed to CEO recipients.`,
            type: 'report',
            user: 'System Scheduler',
            timestamp: new Date().toISOString()
        });
        writeArray(KEYS.activities, activities.slice(0, 1000));

        return true;
    }

    function deleteReport(reportId) {
        const reports = readReports().filter(item => item.id !== reportId);
        writeReports(reports);
    }

    function saveGeneratedReport(payload) {
        const reports = readReports();
        const record = generateReportRecord(payload);
        reports.unshift(record);
        writeReports(reports);
        return record;
    }

    window.CoretexCeo = {
        money,
        pct,
        getKpiSnapshot,
        getProfitMarginTrend,
        getExpenseRatioTrend,
        getExpenseBreakdownCurrent,
        getForecastRows,
        getRiskSummary,
        getBranchComparisonRows,
        getMomData,
        getMonthSeries,
        renderNav,
        mountNotifications,
        makeChart,
        destroyChart,
        fetchNews,
        readReports,
        saveGeneratedReport,
        downloadReport,
        emailReport,
        deleteReport,
        createReportPdfContent
    };
})();
