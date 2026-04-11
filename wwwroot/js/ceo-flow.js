(function () {
    'use strict';

    const KEYS = {
        thresholdAlerts: 'coretex_threshold_alerts',
        activities: 'coretex_activities',
        reports: 'coretex_ceo_reports',
        newsApiKey: 'coretex_news_api_key'
    };

    const SAMPLE = {
        kpi: {
            totalRevenue: 7350000,
            totalExpenses: 4365000,
            netProfit: 2985000,
            profitMargin: 40.6,
            revenueChange: '+12%',
            expensesChange: '+5%',
            profitChange: '+18%'
        },
        branchComparison: [
            { branch: 'Branch 1', revenue: 3000000, expenses: 1500000, profit: 1500000, margin: 50, rank: '🥇' },
            { branch: 'Branch 2', revenue: 2200000, expenses: 1400000, profit: 800000, margin: 36, rank: '🥉' },
            { branch: 'Branch 3', revenue: 2150000, expenses: 1465000, profit: 685000, margin: 32, rank: '🥈' }
        ],
        mom: {
            previous: { label: 'Mar 2026', revenue: 6300000, expenses: 4000000, profit: 2300000 },
            current: { label: 'Apr 2026', revenue: 7350000, expenses: 4365000, profit: 2985000 }
        },
        forecast: [
            { month: 'May 2026', projectedRevenue: 7500000, confidence: 85 },
            { month: 'Jun 2026', projectedRevenue: 7800000, confidence: 75 },
            { month: 'Jul 2026', projectedRevenue: 8000000, confidence: 65 }
        ],
        monthSeries: [
            { month: 'May 2025', revenue: 5900000, expenses: 3800000, cogs: 2200000, rent: 510000, salaries: 720000, utilities: 370000 },
            { month: 'Jun 2025', revenue: 6050000, expenses: 3890000, cogs: 2250000, rent: 510000, salaries: 740000, utilities: 390000 },
            { month: 'Jul 2025', revenue: 6250000, expenses: 3960000, cogs: 2300000, rent: 520000, salaries: 750000, utilities: 390000 },
            { month: 'Aug 2025', revenue: 6400000, expenses: 4015000, cogs: 2330000, rent: 520000, salaries: 770000, utilities: 395000 },
            { month: 'Sep 2025', revenue: 6550000, expenses: 4080000, cogs: 2380000, rent: 530000, salaries: 780000, utilities: 390000 },
            { month: 'Oct 2025', revenue: 6700000, expenses: 4150000, cogs: 2440000, rent: 530000, salaries: 790000, utilities: 390000 },
            { month: 'Nov 2025', revenue: 6840000, expenses: 4210000, cogs: 2480000, rent: 535000, salaries: 800000, utilities: 395000 },
            { month: 'Dec 2025', revenue: 6980000, expenses: 4295000, cogs: 2545000, rent: 540000, salaries: 805000, utilities: 405000 },
            { month: 'Jan 2026', revenue: 7080000, expenses: 4320000, cogs: 2570000, rent: 545000, salaries: 810000, utilities: 395000 },
            { month: 'Feb 2026', revenue: 7160000, expenses: 4355000, cogs: 2590000, rent: 550000, salaries: 815000, utilities: 400000 },
            { month: 'Mar 2026', revenue: 6300000, expenses: 4000000, cogs: 2380000, rent: 500000, salaries: 740000, utilities: 380000 },
            { month: 'Apr 2026', revenue: 7350000, expenses: 4365000, cogs: 2610000, rent: 560000, salaries: 830000, utilities: 365000 }
        ],
        newsFallback: {
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
        }
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

    function monthToDate(monthLabel) {
        return new Date(`${monthLabel} 01`);
    }

    function getMonthSeries() {
        const sorted = [...SAMPLE.monthSeries].sort((a, b) => monthToDate(a.month) - monthToDate(b.month));
        return sorted;
    }

    function getKpiSnapshot() {
        return {
            totalRevenue: SAMPLE.kpi.totalRevenue,
            totalExpenses: SAMPLE.kpi.totalExpenses,
            netProfit: SAMPLE.kpi.netProfit,
            profitMargin: SAMPLE.kpi.profitMargin,
            revenueChange: SAMPLE.kpi.revenueChange,
            expensesChange: SAMPLE.kpi.expensesChange,
            profitChange: SAMPLE.kpi.profitChange,
            risk: getRiskSummary()
        };
    }

    function getProfitMarginTrend() {
        return getMonthSeries().map(item => {
            const profit = item.revenue - item.expenses;
            return {
                month: item.month,
                margin: Number(((profit / item.revenue) * 100).toFixed(1))
            };
        });
    }

    function getExpenseRatioTrend() {
        return getMonthSeries().map(item => ({
            month: item.month,
            ratio: Number(((item.expenses / item.revenue) * 100).toFixed(1))
        }));
    }

    function getExpenseBreakdownCurrent() {
        const current = getMonthSeries()[getMonthSeries().length - 1];
        return {
            cogs: current.cogs,
            rent: current.rent,
            salaries: current.salaries,
            utilities: current.utilities
        };
    }

    function getForecastRows() {
        return SAMPLE.forecast;
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
        return SAMPLE.branchComparison;
    }

    function getMomData() {
        return SAMPLE.mom;
    }

    function getNewsFallback(category) {
        return SAMPLE.newsFallback[category] || SAMPLE.newsFallback.business;
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
