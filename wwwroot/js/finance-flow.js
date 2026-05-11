(function () {
    'use strict';

    const STORAGE_KEYS = {
        users: 'coretex_users',
        branches: 'coretex_branches',
        posTransactions: 'coretex_pos_transactions',
        expenses: 'coretex_finance_expenses',
        submissions: 'coretex_submissions',
        monthlyData: 'coretex_monthly_data',
        monthlySummaries: 'coretex_monthly_summaries',
        kpiThresholds: 'coretex_kpi_thresholds',
        thresholdAlerts: 'coretex_threshold_alerts',
        activities: 'coretex_activities',
        auditLog: 'coretex_audit_log',
        adminUnlocks: 'coretex_finance_admin_unlocks'
    };

    const CATEGORY_LABELS = {
        cogs: 'COGS',
        rent: 'Rent',
        salaries: 'Salaries',
        utilities: 'Utilities'
    };

    const FALLBACK_RATES = {
        PHP: 1,
        USD: 56.5,
        CNY: 7.85,
        EUR: 61.2,
        JPY: 0.38
    };

    function readArray(key) {
        try {
            const value = JSON.parse(localStorage.getItem(key) || '[]');
            return Array.isArray(value) ? value : [];
        } catch (_error) {
            return [];
        }
    }

    function readObject(key) {
        try {
            const value = JSON.parse(localStorage.getItem(key) || '{}');
            return value && typeof value === 'object' && !Array.isArray(value) ? value : {};
        } catch (_error) {
            return {};
        }
    }

    function write(key, value) {
        localStorage.setItem(key, JSON.stringify(value));
    }

    function uid(prefix) {
        return `${prefix}_${Date.now()}_${Math.random().toString(36).slice(2, 8)}`;
    }

    function toNumber(value) {
        const parsed = Number(value);
        return Number.isFinite(parsed) ? parsed : 0;
    }

    function toMoney(value) {
        return Number(toNumber(value).toFixed(2));
    }

    function percent(value) {
        return Number(toNumber(value).toFixed(2));
    }

    function ensureText(value) {
        return String(value || '').trim();
    }

    function toDateKey(date) {
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, '0');
        const day = String(date.getDate()).padStart(2, '0');
        return `${year}-${month}-${day}`;
    }

    function toMonthKey(date) {
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, '0');
        return `${year}-${month}`;
    }

    function formatMonthLabel(monthKey) {
        const [year, month] = String(monthKey || '').split('-').map(Number);
        if (!year || !month) return 'Unknown Month';

        return new Date(year, month - 1, 1).toLocaleDateString('en-US', {
            month: 'long',
            year: 'numeric'
        });
    }

    function formatCurrency(value) {
        return `\u20B1${toNumber(value).toLocaleString('en-PH', {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2
        })}`;
    }

    function formatDateTime(iso) {
        const date = new Date(iso);
        if (Number.isNaN(date.getTime())) return '-';

        return date.toLocaleString('en-US', {
            month: 'short',
            day: 'numeric',
            year: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        });
    }

    function monthsEqual(timestamp, monthKey) {
        const date = new Date(timestamp);
        if (Number.isNaN(date.getTime())) return false;
        return toMonthKey(date) === monthKey;
    }

    function getContext() {
        const users = readArray(STORAGE_KEYS.users);
        const branches = readArray(STORAGE_KEYS.branches);

        const financeOfficer = users.find(user => user.role === 'Finance Officer' && user.status === 'Active')
            || users.find(user => user.role === 'Finance Officer')
            || { id: 'fo_demo', name: 'Demo Finance Officer', branchId: null, role: 'Finance Officer' };

        const branch = branches.find(item => item.id === financeOfficer.branchId)
            || branches[0]
            || { id: 'branch_default', name: 'Main Branch' };

        return {
            financeOfficer: {
                id: financeOfficer.id || 'fo_demo',
                name: financeOfficer.name || 'Demo Finance Officer',
                role: financeOfficer.role || 'Finance Officer'
            },
            branch: {
                id: branch.id || 'branch_default',
                name: branch.name || 'Main Branch'
            }
        };
    }

    function getCurrentMonthKey() {
        return toMonthKey(new Date());
    }

    function getPosTransactions(monthKey, branchId) {
        const records = readArray(STORAGE_KEYS.posTransactions);

        return records
            .filter(record => !record.deleted)
            .filter(record => monthsEqual(record.timestamp, monthKey))
            .filter(record => !branchId || (record.branchId || 'branch_default') === branchId);
    }

    function getSalesSummary(monthKey, branchId) {
        const records = getPosTransactions(monthKey, branchId);

        const totalRevenue = toMoney(records.reduce((sum, record) => sum + toNumber(record.lineTotal), 0));
        const totalTransactions = records.length;

        const productQty = {};
        records.forEach(record => {
            const name = ensureText(record.productName) || 'Unknown Product';
            productQty[name] = (productQty[name] || 0) + toNumber(record.quantity);
        });

        const topProductEntry = Object.entries(productQty).sort((a, b) => b[1] - a[1])[0] || ['-', 0];

        return {
            totalTransactions,
            totalRevenue,
            topProduct: topProductEntry[0],
            topProductQty: topProductEntry[1],
            records
        };
    }

    function getAllExpenseRecords(monthKey, branchId) {
        const records = readArray(STORAGE_KEYS.expenses);

        return records
            .filter(record => !record.deleted)
            .filter(record => record.monthKey === monthKey)
            .filter(record => !branchId || record.branchId === branchId);
    }

    function getExpenseRecord(monthKey, category, branchId) {
        return getAllExpenseRecords(monthKey, branchId).find(record => record.category === category) || null;
    }

    function getExpenseBreakdown(monthKey, branchId) {
        const records = getAllExpenseRecords(monthKey, branchId);

        const breakdown = {
            cogs: 0,
            rent: 0,
            salaries: 0,
            utilities: 0,
            total: 0
        };

        records.forEach(record => {
            const category = record.category;
            if (breakdown[category] !== undefined) {
                breakdown[category] += toNumber(record.amountPHP);
            }
        });

        breakdown.cogs = toMoney(breakdown.cogs);
        breakdown.rent = toMoney(breakdown.rent);
        breakdown.salaries = toMoney(breakdown.salaries);
        breakdown.utilities = toMoney(breakdown.utilities);
        breakdown.total = toMoney(breakdown.cogs + breakdown.rent + breakdown.salaries + breakdown.utilities);

        return breakdown;
    }

    async function fetchSnapshot(monthKey) {
        const [year, month] = monthKey.split('-').map(Number);
        try {
            const response = await fetch(`/Finance/GetFinanceSnapshot?year=${year}&month=${month}`);
            if (!response.ok) throw new Error('Failed to fetch finance snapshot');
            const data = await response.json();
            return {
                monthKey,
                sales: {
                    totalRevenue: data.totalRevenue,
                    totalTransactions: data.totalTransactions,
                    topProduct: data.topProduct,
                    topProductQty: data.topProductQty
                },
                expenses: data.expenseBreakdown,
                totalTransactions: data.totalTransactions,
                totalRevenue: data.totalRevenue,
                topProduct: data.topProduct,
                topProductQty: data.topProductQty,
                netProfit: data.netProfit,
                submissionStatus: data.submissionStatus,
                isLocked: data.isLocked,
                submissionDate: data.submissionDate,
                dataSources: ['SQL Database', 'Exchange API']
            };
        } catch (error) {
            console.error('Snapshot Fetch Error:', error);
            return getSnapshot(monthKey); // Fallback to local
        }
    }

    function getSnapshot(monthKey, branchId) {
        const sales = getSalesSummary(monthKey, branchId);
        const expenses = getExpenseBreakdown(monthKey, branchId);
        const netProfit = toMoney(sales.totalRevenue - expenses.total);

        return {
            monthKey,
            sales,
            expenses,
            totalTransactions: sales.totalTransactions,
            totalRevenue: sales.totalRevenue,
            topProduct: sales.topProduct,
            topProductQty: sales.topProductQty,
            netProfit,
            expenseRatioPercent: sales.totalRevenue > 0 ? percent((expenses.total / sales.totalRevenue) * 100) : 0,
            profitMarginPercent: sales.totalRevenue > 0 ? percent((netProfit / sales.totalRevenue) * 100) : 0,
            dataSources: ['POS table', 'Expense table', 'System logs']
        };
    }

    function getSubmission(monthKey, branchId) {
        const submissions = readArray(STORAGE_KEYS.submissions);
        return submissions.find(item => item.month === monthKey && item.branchId === branchId) || null;
    }

    function hasAdminUnlock(monthKey, branchId) {
        const unlocks = readArray(STORAGE_KEYS.adminUnlocks);
        return unlocks.some(item => item.month === monthKey && item.branchId === branchId && item.unlocked === true);
    }

    function canEditSubmission(monthKey, branchId) {
        const submission = getSubmission(monthKey, branchId);
        if (!submission) return false;

        const submittedAt = new Date(submission.submittedAt).getTime();
        if (!Number.isFinite(submittedAt)) return false;

        const within48Hours = Date.now() <= submittedAt + (48 * 60 * 60 * 1000);
        const unlocked = Boolean(submission.adminUnlocked) || hasAdminUnlock(monthKey, branchId);

        return within48Hours || unlocked;
    }

    function isMonthLocked(monthKey, branchId) {
        const submission = getSubmission(monthKey, branchId);
        return Boolean(submission && submission.locked);
    }

    function logActivity(action, description, type, userName) {
        const activities = readArray(STORAGE_KEYS.activities);
        activities.unshift({
            id: uid('activity'),
            action,
            description,
            type: type || 'finance',
            user: userName || 'Finance Officer',
            timestamp: new Date().toISOString()
        });

        write(STORAGE_KEYS.activities, activities.slice(0, 1000));
    }

    function logAudit(entry) {
        const logs = readArray(STORAGE_KEYS.auditLog);
        logs.unshift({
            id: uid('audit'),
            timestamp: new Date().toISOString(),
            user: entry.user || 'Finance Officer',
            module: entry.module || 'Finance',
            action: entry.action || 'Updated',
            fieldChanged: entry.fieldChanged || '-',
            oldValue: entry.oldValue === undefined ? '-' : String(entry.oldValue),
            newValue: entry.newValue === undefined ? '-' : String(entry.newValue),
            recordId: entry.recordId || '-',
            details: entry.details || ''
        });

        write(STORAGE_KEYS.auditLog, logs.slice(0, 2000));
    }

    function normalizeExpensePayload(category, payload) {
        const currency = ensureText(payload.currency || 'PHP').toUpperCase();
        const exchangeRate = toMoney(payload.exchangeRate || FALLBACK_RATES[currency] || FALLBACK_RATES.USD || 1);

        const baseAmount = payload.originalAmount !== undefined
            ? toMoney(payload.originalAmount)
            : toMoney(payload.amount);

        const amountPHP = payload.amountPHP !== undefined
            ? toMoney(payload.amountPHP)
            : toMoney(baseAmount * exchangeRate);

        return {
            category,
            currency,
            exchangeRate,
            originalAmount: baseAmount,
            amountPHP,
            description: ensureText(payload.description),
            notes: ensureText(payload.notes)
        };
    }

    function diffExpense(oldRecord, newRecord) {
        if (!oldRecord) {
            return [{
                field: 'record',
                oldValue: '-',
                newValue: `${CATEGORY_LABELS[newRecord.category]} ${formatCurrency(newRecord.amountPHP)}`
            }];
        }

        const tracked = [
            ['currency', 'Currency'],
            ['originalAmount', 'Original Amount'],
            ['exchangeRate', 'Exchange Rate'],
            ['amountPHP', 'Converted Amount (PHP)'],
            ['description', 'Description'],
            ['notes', 'Notes']
        ];

        const changes = [];

        tracked.forEach(([field, label]) => {
            const oldValue = oldRecord[field] === undefined || oldRecord[field] === null ? '' : String(oldRecord[field]);
            const newValue = newRecord[field] === undefined || newRecord[field] === null ? '' : String(newRecord[field]);

            if (oldValue !== newValue) {
                changes.push({
                    field: label,
                    oldValue: oldValue || '-',
                    newValue: newValue || '-'
                });
            }
        });

        return changes;
    }

    async function saveExpense(category, payload, options) {
        if (!CATEGORY_LABELS[category]) {
            throw new Error('Unknown expense category.');
        }

        const context = getContext();
        const monthKey = ensureText(options && options.monthKey) || getCurrentMonthKey();
        const branchId = ensureText(options && options.branchId) || context.branch.id;
        const branchName = ensureText(options && options.branchName) || context.branch.name;
        const reason = ensureText(options && options.reason);
        const allowLocked = Boolean(options && options.allowLocked);

        if (isMonthLocked(monthKey, branchId) && !allowLocked) {
            throw new Error(`Expenses are locked for ${formatMonthLabel(monthKey)}. Use Edit Submitted Data.`);
        }

        const normalized = normalizeExpensePayload(category, payload || {});

        // AUDIT ENFORCEMENT (Strategic Guardrail)
        const isAuditActive = localStorage.getItem('coretex_overhead_audit') === 'true';
        if (isAuditActive && normalized.amountPHP > 5000) {
            const justification = prompt("⚠️ STRATEGIC AUDIT ACTIVE\nExpenses over ₱5,000 require an Executive Justification during an active audit. Please state the purpose of this expenditure:");
            if (!justification || justification.trim().length < 5) {
                throw new Error("Submission Rejected: A valid justification is required to bypass the cost-cutting protocol.");
            }
            normalized.notes = `[AUDIT JUSTIFIED: ${justification.trim()}] ${normalized.notes || ''}`;
            normalized.description = `[AUDIT] ${normalized.description || ''}`;
        }

        // CALL BACKEND
        try {
            const response = await fetch('/Finance/CreateExpense', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    description: normalized.description || `Monthly ${CATEGORY_LABELS[category]}`,
                    category: CATEGORY_LABELS[category],
                    amount: normalized.originalAmount,
                    currency: normalized.currency,
                    date: new Date().toISOString()
                })
            });

            const result = await response.json();
            if (!result.success) {
                throw new Error(result.errors ? result.errors.join(', ') : 'Server failed to save expense.');
            }
            
            // If successful, we can also sync to localStorage for the "Summary Table" legacy views 
            // if you want to keep them working without a full page refresh.
            const expenses = readArray(STORAGE_KEYS.expenses);
            const index = expenses.findIndex(item => item.monthKey === monthKey && item.branchId === branchId && item.category === category && !item.deleted);
            const existing = index >= 0 ? expenses[index] : null;

            const record = {
                id: result.id || (existing ? existing.id : uid('finexp')),
                monthKey,
                category,
                currency: normalized.currency,
                exchangeRate: normalized.exchangeRate,
                originalAmount: normalized.originalAmount,
                amountPHP: result.amount || normalized.amountPHP,
                description: normalized.description,
                notes: normalized.notes,
                branchId,
                branchName,
                createdAt: existing ? existing.createdAt : new Date().toISOString(),
                updatedAt: new Date().toISOString()
            };

            if (existing) {
                expenses[index] = record;
            } else {
                expenses.unshift(record);
            }

            write(STORAGE_KEYS.expenses, expenses.slice(0, 2000));
            return { record, created: !existing };

        } catch (error) {
            console.error('Expense Save Error:', error);
            throw new Error('Database Error: ' + error.message);
        }
    }

    function upsertSubmission(submission) {
        const submissions = readArray(STORAGE_KEYS.submissions);
        const index = submissions.findIndex(item => item.month === submission.month && item.branchId === submission.branchId);

        if (index >= 0) {
            submissions[index] = submission;
        } else {
            submissions.unshift(submission);
        }

        write(STORAGE_KEYS.submissions, submissions.slice(0, 1000));
    }

    function upsertSummaryTables(submission, snapshot) {
        const [year, month] = submission.month.split('-').map(Number);
        const monthDate = new Date(year, month - 1, 1);
        const monthName = monthDate.toLocaleDateString('en-US', { month: 'long' });

        const summaryProfitMargin = submission.totalSales > 0
            ? percent((submission.netProfit / submission.totalSales) * 100)
            : 0;

        const monthlySummaries = readArray(STORAGE_KEYS.monthlySummaries);
        const summaryIndex = monthlySummaries.findIndex(item => Number(item.month) === month && Number(item.year) === year && item.branchId === submission.branchId);

        const summaryEntry = {
            id: summaryIndex >= 0 ? monthlySummaries[summaryIndex].id : uid('monthsum'),
            month,
            year,
            branchId: submission.branchId,
            branchName: submission.branchName,
            totalSales: submission.totalSales,
            totalExpenses: submission.totalExpenses,
            totalImports: snapshot.expenses.cogs,
            customers: snapshot.totalTransactions,
            profit: submission.netProfit,
            profitMargin: summaryProfitMargin,
            status: submission.status,
            submittedAt: submission.submittedAt,
            updatedAt: submission.updatedAt
        };

        if (summaryIndex >= 0) {
            monthlySummaries[summaryIndex] = summaryEntry;
        } else {
            monthlySummaries.unshift(summaryEntry);
        }

        write(STORAGE_KEYS.monthlySummaries, monthlySummaries.slice(0, 1000));

        const monthlyData = readArray(STORAGE_KEYS.monthlyData);
        const dataIndex = monthlyData.findIndex(item => item.month === monthName && String(item.year) === String(year) && item.branchId === submission.branchId);

        const dataEntry = {
            month: monthName,
            year: String(year),
            sales: submission.totalSales,
            expenses: submission.totalExpenses,
            importedPHP: snapshot.expenses.cogs,
            customers: snapshot.totalTransactions,
            profit: submission.netProfit,
            profitMargin: summaryProfitMargin,
            branchId: submission.branchId,
            branchName: submission.branchName,
            timestamp: submission.updatedAt
        };

        if (dataIndex >= 0) {
            monthlyData[dataIndex] = dataEntry;
        } else {
            monthlyData.unshift(dataEntry);
        }

        write(STORAGE_KEYS.monthlyData, monthlyData.slice(0, 1000));
    }

    function evaluateKpiBreaches(submission, snapshot) {
        const thresholds = readObject(STORAGE_KEYS.kpiThresholds);

        const minProfitMargin = thresholds.minProfitMargin;
        const maxExpenseRatio = thresholds.maxExpenseRatio;
        const minMonthlyProfit = thresholds.minMonthlyProfit;

        const breaches = [];

        if (typeof minProfitMargin === 'number' && snapshot.profitMarginPercent < minProfitMargin) {
            breaches.push({
                metric: 'Profit Margin',
                message: `Profit margin is ${snapshot.profitMarginPercent}% (minimum is ${minProfitMargin}%).`,
                severity: thresholds.riskAlertLevel || 'yellow'
            });
        }

        if (typeof maxExpenseRatio === 'number' && snapshot.expenseRatioPercent > maxExpenseRatio) {
            breaches.push({
                metric: 'Expense Ratio',
                message: `Expense ratio is ${snapshot.expenseRatioPercent}% (maximum is ${maxExpenseRatio}%).`,
                severity: thresholds.riskAlertLevel || 'red'
            });
        }

        if (typeof minMonthlyProfit === 'number' && submission.netProfit < minMonthlyProfit) {
            breaches.push({
                metric: 'Monthly Profit',
                message: `Net profit is ${formatCurrency(submission.netProfit)} (minimum is ${formatCurrency(minMonthlyProfit)}).`,
                severity: thresholds.riskAlertLevel || 'red'
            });
        }

        if (breaches.length > 0) {
            const alerts = readArray(STORAGE_KEYS.thresholdAlerts);
            const nowIso = new Date().toISOString();

            breaches.forEach(breach => {
                alerts.unshift({
                    id: uid('alert'),
                    metric: breach.metric,
                    message: `${breach.message} Branch: ${submission.branchName} | Month: ${formatMonthLabel(submission.month)}`,
                    severity: breach.severity,
                    timestamp: nowIso
                });
            });

            write(STORAGE_KEYS.thresholdAlerts, alerts.slice(0, 500));
        }

        return breaches;
    }

    async function submitMonth(monthKey) {
        const context = getContext();
        const branchId = context.branch.id;
        const existing = getSubmission(monthKey, branchId);

        if (existing && existing.locked && !canEditSubmission(monthKey, branchId)) {
            throw new Error(`Data for ${formatMonthLabel(monthKey)} is already submitted and locked.`);
        }

        const [year, month] = monthKey.split('-').map(Number);
        
        try {
            const response = await fetch('/Finance/SubmitMonth', {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                body: new URLSearchParams({
                    year,
                    month,
                    notes: 'Submitted via Coretex Finance Flow'
                })
            });

            const result = await response.json();
            if (!result.success) {
                throw new Error('Server failed to finalize submission.');
            }

            // Sync localStorage for immediate UI update
            const snapshot = getSnapshot(monthKey, branchId);
            const nowIso = new Date().toISOString();

            const submission = {
                id: result.submissionId || uid('submission'),
                month: monthKey,
                branchId,
                branchName: context.branch.name,
                financeOfficerId: context.financeOfficer.id,
                financeOfficerName: context.financeOfficer.name,
                totalSales: snapshot.totalRevenue,
                totalExpenses: snapshot.expenses.total,
                netProfit: snapshot.netProfit,
                totalTransactionCount: snapshot.totalTransactions,
                topProduct: snapshot.topProduct,
                status: 'Submitted',
                locked: true,
                submittedAt: nowIso,
                updatedAt: nowIso,
                editWindowUntil: new Date(Date.now() + (48 * 60 * 60 * 1000)).toISOString(),
                summarySnapshot: snapshot
            };

            upsertSubmission(submission);
            upsertSummaryTables(submission, snapshot);

            return {
                submission,
                snapshot,
                monthLabel: formatMonthLabel(monthKey)
            };

        } catch (error) {
            console.error('Submission Error:', error);
            throw new Error('Database Error: ' + error.message);
        }
    }

    function getEditDiff(monthKey, edits, branchId) {
        const changedFields = [];

        Object.keys(CATEGORY_LABELS).forEach(category => {
            const edit = edits[category];
            if (!edit) return;

            const existing = getExpenseRecord(monthKey, category, branchId);
            const normalized = normalizeExpensePayload(category, edit);

            const draft = {
                id: existing ? existing.id : uid('draft'),
                monthKey,
                category,
                currency: normalized.currency,
                exchangeRate: normalized.exchangeRate,
                originalAmount: normalized.originalAmount,
                amountPHP: normalized.amountPHP,
                description: normalized.description,
                notes: normalized.notes
            };

            const diffs = diffExpense(existing, draft);
            diffs.forEach(diff => {
                changedFields.push(`${CATEGORY_LABELS[category]} - ${diff.field}`);
            });
        });

        return changedFields;
    }

    function saveEditedExpenses(monthKey, edits, reason) {
        const context = getContext();
        const branchId = context.branch.id;
        const submission = getSubmission(monthKey, branchId);

        if (!submission) {
            throw new Error('No submitted data was found for this month.');
        }

        if (!canEditSubmission(monthKey, branchId)) {
            throw new Error('Editing is allowed only within 48 hours or when admin unlock is enabled.');
        }

        const cleanReason = ensureText(reason);
        if (!cleanReason) {
            throw new Error('Reason for edit is required.');
        }

        let changedFields = [];

        Object.keys(CATEGORY_LABELS).forEach(category => {
            const edit = edits[category];
            if (!edit) return;

            const result = saveExpense(category, edit, {
                monthKey,
                branchId,
                branchName: context.branch.name,
                allowLocked: true,
                reason: cleanReason
            });

            result.changes.forEach(change => {
                changedFields.push(`${CATEGORY_LABELS[category]} - ${change.field}`);
            });
        });

        changedFields = [...new Set(changedFields)];

        if (changedFields.length === 0) {
            throw new Error('No field changes were detected.');
        }

        const snapshot = getSnapshot(monthKey, branchId);
        const nowIso = new Date().toISOString();

        const updatedSubmission = {
            ...submission,
            totalSales: snapshot.totalRevenue,
            totalExpenses: snapshot.expenses.total,
            netProfit: snapshot.netProfit,
            totalTransactionCount: snapshot.totalTransactions,
            topProduct: snapshot.topProduct,
            status: 'Edited',
            locked: true,
            updatedAt: nowIso,
            editedAt: nowIso,
            editReason: cleanReason,
            changedFields,
            summarySnapshot: snapshot
        };

        upsertSubmission(updatedSubmission);
        upsertSummaryTables(updatedSubmission, snapshot);

        logActivity(
            'Submission Edited',
            `${formatMonthLabel(monthKey)} submission edited. Reason: ${cleanReason}`,
            'submission',
            context.financeOfficer.name
        );

        logAudit({
            user: context.financeOfficer.name,
            module: 'Finance Submission',
            action: 'Updated Submission',
            fieldChanged: 'editReason',
            oldValue: submission.editReason || '-',
            newValue: cleanReason,
            recordId: updatedSubmission.id,
            details: `Fields changed: ${changedFields.join(', ')}`
        });

        logAudit({
            user: context.financeOfficer.name,
            module: 'Finance Submission',
            action: 'Updated Submission',
            fieldChanged: 'netProfit',
            oldValue: formatCurrency(submission.netProfit),
            newValue: formatCurrency(updatedSubmission.netProfit),
            recordId: updatedSubmission.id,
            details: `Fields changed: ${changedFields.join(', ')}`
        });

        const breaches = evaluateKpiBreaches(updatedSubmission, snapshot);

        if (breaches.length > 0) {
            logActivity(
                'Threshold Alert Triggered',
                `KPI threshold breached after editing ${formatMonthLabel(monthKey)}. Twilio SMS alert sent to CEO (simulated).`,
                'submission',
                context.financeOfficer.name
            );
        }

        return {
            submission: updatedSubmission,
            snapshot,
            changedFields,
            breaches
        };
    }

    async function fetchSubmissionHistory() {
        try {
            const response = await fetch('/Finance/GetSubmissionHistory');
            if (!response.ok) throw new Error('Failed to fetch history');
            return await response.json();
        } catch (error) {
            console.error('History Fetch Error:', error);
            return [];
        }
    }

    function getSubmissionHistory(branchId) {
        return readArray(STORAGE_KEYS.submissions)
            .filter(item => !branchId || item.branchId === branchId)
            .sort((a, b) => new Date(b.submittedAt) - new Date(a.submittedAt));
    }

    function getSubmissionDetailsById(id) {
        const submissions = readArray(STORAGE_KEYS.submissions);
        const found = submissions.find(item => item.id === id);
        if (!found) return null;

        return {
            submission: found,
            monthLabel: formatMonthLabel(found.month),
            snapshot: found.summarySnapshot || getSnapshot(found.month, found.branchId),
            expenses: getExpenseBreakdown(found.month, found.branchId)
        };
    }

    async function fetchExchangeRate(currency) {
        const clean = ensureText(currency || 'PHP').toUpperCase();
        if (clean === 'PHP') {
            return { rate: 1, source: 'local' };
        }

        try {
            // Securely call our own backend API instead of the public one
            const response = await fetch(`/Finance/GetExchangeRate?currency=${clean}`);
            if (!response.ok) {
                throw new Error('Failed to load secure exchange rate from backend.');
            }

            const data = await response.json();
            const rate = toNumber(data && data.rate ? data.rate : 0);
            if (!rate) {
                throw new Error('Missing conversion rate from backend.');
            }

            return { rate: toMoney(rate), source: 'Live Server API' };
        } catch (_error) {
            console.error('Exchange Rate Error:', _error);
            return { rate: toMoney(FALLBACK_RATES[clean] || FALLBACK_RATES.USD), source: 'cached' };
        }
    }

    async function getDashboardData(monthKey) {
        const context = getContext();
        const snapshot = await fetchSnapshot(monthKey);
        
        let submissionStatus = snapshot.submissionStatus || 'Not Submitted';
        if (snapshot.submissionDate) {
            submissionStatus = `Submitted on ${formatDateTime(snapshot.submissionDate)}`;
        }

        return {
            context,
            snapshot,
            submission: snapshot.isLocked ? { status: snapshot.submissionStatus, submittedAt: snapshot.submissionDate } : null,
            submissionStatus
        };
    }

    window.CoretexFinanceFlow = {
        categoryLabels: CATEGORY_LABELS,
        storageKeys: STORAGE_KEYS,
        formatCurrency,
        formatDateTime,
        formatMonthLabel,
        toDateKey,
        toMonthKey,
        getCurrentMonthKey,
        getContext,
        getDashboardData,
        fetchSnapshot,
        getSnapshot,
        getSalesSummary,
        getAllExpenseRecords,
        getExpenseRecord,
        getExpenseBreakdown,
        getSubmission,
        fetchSubmissionHistory,
        getSubmissionHistory,
        getSubmissionDetailsById,
        isMonthLocked,
        canEditSubmission,
        hasAdminUnlock,
        getEditDiff,
        saveExpense,
        saveEditedExpenses,
        submitMonth,
        fetchExchangeRate
    };
})();
