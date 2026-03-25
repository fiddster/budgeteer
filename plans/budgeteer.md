# Plan: Budgeteer

> Source PRD: GitHub issue #1 — PRD: Budgeteer -- Local-First Personal Finance App

## Architectural decisions

- **Platform**: Windows desktop via .NET MAUI; iOS/Android out of scope
- **Navigation**: Shell-based with tabs — Accounts, Transactions, Rules, Budgets, Dashboard
- **UI pattern**: MVVM; each feature gets its own Page + ViewModel
- **Schema**: Settled in Phase 1 — `Account`, `ColumnMapping`, `Transaction`, `Category`, `CategorizationRule`, `Budget`, `BudgetCategory`
- **Data storage**: SQLite via EF Core, stored in `FileSystem.AppDataDirectory`
- **CSV parsing**: CsvHelper (already referenced in `Directory.Packages.props`)
- **Charts**: Microcharts (MIT license, SkiaSharp-based) — one pie chart per active budget
- **Transfers**: Flagged at the transaction level (`IsTransfer` bool); excluded from all spending and budget calculations
- **Auto-categorization**: Rules evaluated in user-defined precedence order; first match wins; unmatched transactions remain uncategorized
- **Budget periods**: Calculated dynamically at query time — no pre-computed snapshots
- **Duplicate detection**: Match on Date + Description + Amount + AccountId

---

## Phase 1: Foundation ✅

**User stories**: n/a (infrastructure)

### What to build

Domain entities, EF Core + SQLite data layer, `DatabaseInitializer` (creates schema + seeds 14 default categories), `ICategoryService`/`CategoryService`, `IAccountService`/`AccountService`, DI wiring in `MauiProgram.cs`.

### Acceptance criteria

- [x] All domain entities exist and EF Core schema is created on first launch
- [x] 14 default categories seeded idempotently
- [x] Category CRUD works (Add, Rename, Delete, GetAll)
- [x] Account CRUD works (Add, Update, Delete, GetAll)
- [x] Column mapping saved and retrieved per account
- [x] 15 passing unit tests using real SQLite `:memory:`

---

## Phase 2: CSV Import Pipeline

**User stories**: 2 (import CSV), 3 (map columns), 4 (remember mapping), 5 (preview), 6 (duplicate warning)

### What to build

A multi-step import wizard accessible from an account's detail page:

1. **File selection** — file picker filtered to `.csv`; parse headers with CsvHelper
2. **Column mapping** — user maps CSV headers to: Date, Description, Amount, Balance (optional), Reference (optional); pre-filled from saved `ColumnMapping` if one exists
3. **Parse + duplicate detection** — parse all rows using the mapping; flag rows that match an existing transaction (Date + Description + Amount + AccountId)
4. **Preview screen** — show parsed transactions in a list; duplicates highlighted with a skip/import toggle; user can review before committing
5. **Commit** — write accepted transactions to DB; save column mapping for the account; transactions land as uncategorized (`CategoryId = null`, `IsTransfer = false`)

No auto-categorization in this phase — that is wired in Phase 5.

### Acceptance criteria

- [ ] Selecting a CSV file parses its headers and presents the column mapping UI
- [ ] Column mapping is pre-filled from a saved mapping if one exists for the account
- [ ] After mapping, all rows are parsed into candidate transactions
- [ ] Duplicate transactions (Date + Description + Amount + Account) are flagged in the preview
- [ ] User can toggle skip/import on individual duplicates
- [ ] Committing writes accepted transactions to the database as uncategorized
- [ ] Column mapping is persisted to the account after a successful import
- [ ] Integration tests: given a CSV string + mapping, assert correct transactions are parsed
- [ ] Integration tests: duplicate detection correctly identifies duplicates

---

## Phase 3: Transaction List & Transfers

**User stories**: 24 (view transactions per account), 25 (filter by date/category/account), 22 (mark as transfer), 23 (transfers excluded from calculations)

### What to build

A Transactions page showing all transactions across accounts, filterable by date range, category, and account. Each transaction row has a context action to mark/unmark it as a transfer. Transfers are visually distinguished (e.g. a label or icon). The `IsTransfer` flag is already on the entity; this phase surfaces it in the UI and ensures the filter/query layer respects it.

### Acceptance criteria

- [ ] Transactions page lists all transactions ordered by date descending
- [ ] Filter by account shows only that account's transactions
- [ ] Filter by category shows only transactions with that category
- [ ] Filter by date range narrows results correctly
- [ ] A transaction can be marked as a transfer; the flag persists
- [ ] Transfers are visually distinguished in the list
- [ ] Unit tests: transfer-flagged transactions are excluded from spending queries

---

## Phase 4: Manual Categorization & Rule Creation Prompt

**User stories**: 8 (uncategorized transactions visible), 9 (manually assign/change category), 10 (offer to create rule after manual categorization)

### What to build

Tapping a transaction opens a categorization picker (category list + "Uncategorized" option). After the user selects a category, if the transaction was previously uncategorized the app prompts: _"Always categorize '[Description keyword]' as '[Category]'?"_ — Yes creates a `CategorizationRule`; No skips. Uncategorized transactions are surfaced with a clear visual indicator in the transaction list.

### Acceptance criteria

- [ ] Uncategorized transactions are visually distinct in the list
- [ ] Tapping a transaction opens a category picker showing all categories
- [ ] Selecting a category updates the transaction immediately
- [ ] After categorizing an uncategorized transaction, a rule-creation prompt appears
- [ ] Confirming the prompt creates a `CategorizationRule` for the keyword (extracted from Description) → Category
- [ ] Declining the prompt does not create a rule
- [ ] Unit tests: rule creation stores correct keyword, category, and precedence

---

## Phase 5: Categorization Rule Management & Auto-Categorization

**User stories**: 7 (auto-categorize on import), 11 (create and manage rules)

### What to build

A Rules page for CRUD management of `CategorizationRule` entries — list rules, add manually (keyword + category), reorder (drag or up/down arrows to set precedence), delete. A `CategorizationEngine` service applies rules to a list of transactions: evaluates rules in `Precedence` order, case-insensitive `Description.Contains(keyword)`, first match wins. The engine is wired into the import commit step (Phase 2) so imported transactions are auto-categorized before being written to the database.

### Acceptance criteria

- [ ] Rules page lists all rules ordered by precedence
- [ ] User can add a rule (keyword + category)
- [ ] User can reorder rules (precedence updated on reorder)
- [ ] User can delete a rule
- [ ] Unit tests: given rules + transaction description → correct category assigned
- [ ] Unit tests: rule precedence — first match wins
- [ ] Unit tests: case-insensitive matching
- [ ] Unit tests: no match → transaction remains uncategorized
- [ ] Imported transactions are auto-categorized using the engine before being saved

---

## Phase 6: Budget Management

**User stories**: 14 (create budget), 15 (assign categories), 16 (timespan), 17 (rollover), 18 (alert config)

### What to build

A Budgets page with a list of budgets and a Create/Edit form. Fields: name, spending limit, timespan (Weekly/Monthly/Yearly), rollover toggle, alert toggle + optional alert threshold percentage. A category assignment UI (checklist or multi-select) maps one or more categories to the budget via `BudgetCategory`.

### Acceptance criteria

- [ ] User can create a budget with name, limit, timespan, rollover, and alert settings
- [ ] User can assign one or more categories to a budget
- [ ] User can edit and delete a budget
- [ ] Budgets page lists all budgets with their assigned categories
- [ ] Unit tests: budget CRUD via service

---

## Phase 7: Budget Period Engine

**User stories**: *(backend for dashboard — no direct user story, enables US 19, 20, 23)*

### What to build

A `BudgetPeriodEngine` service that, given a budget and a reference date, computes:

- The current period window (start/end date based on timespan)
- Total spending in the period: sum of `Amount` for transactions where `CategoryId` is in the budget's categories, `IsTransfer = false`, and `Date` is within the window
- Rollover amount: unspent budget from the previous period (only when `Rollover = true`)
- Effective limit: `SpendingLimit + rolloverAmount`
- Remaining: `effectiveLimit - spending` (can be negative = overspent)
- Alert triggered: `AlertEnabled = true` AND spending ≥ `AlertThresholdPercent`% of effective limit

### Acceptance criteria

- [ ] Unit tests: correct period window for weekly/monthly/yearly timespans
- [ ] Unit tests: spending total excludes transfers and out-of-period transactions
- [ ] Unit tests: rollover adds previous period unspent amount to current limit
- [ ] Unit tests: no rollover when `Rollover = false`
- [ ] Unit tests: alert triggered at threshold; not triggered below threshold
- [ ] Unit tests: period boundary edge cases (first/last day of period)
- [ ] Unit tests: empty period returns zero spending

---

## Phase 8: Dashboard

**User stories**: 19 (budget pie charts), 20 (category spending breakdown), 21 (uncategorized transactions widget)

### What to build

A Dashboard page composed of three sections:

1. **Budget health** — one Microcharts pie chart per active budget showing spent % vs. remaining %. Budgets over limit shown in a warning colour. Alert indicator when threshold is triggered.
2. **Category spending breakdown** — a list or bar chart of spending per category for the current period (using the most common timespan, or a selectable period).
3. **Uncategorized transactions widget** — a short list of recent uncategorized, non-transfer transactions with a tap-to-categorize shortcut.

### Acceptance criteria

- [ ] Dashboard loads and displays all active budgets as pie charts
- [ ] Each pie chart shows correct spent/remaining percentages from `BudgetPeriodEngine`
- [ ] Overspent budgets are visually highlighted
- [ ] Category spending breakdown shows correct totals for the current period
- [ ] Uncategorized widget lists recent uncategorized transactions
- [ ] Tapping an uncategorized transaction navigates to the categorization picker
