# NextLedger - Personal Envelope Budgeting App

## Vision

A Windows-first personal finance app that brings the proven envelope budgeting methodology to the desktop. Privacy-focused, offline-first, and designed for users who want control over their money without sharing data with third parties.

---

## Current Status (Feb 2026)

- The deterministic core and budget engine orchestration are in place (Phase 2).
- Phase 3 focuses on exposing this functionality through a UI that treats the engine contract as the source of truth.

Key Phase 3 guardrails:
- UI must consume `BudgetSnapshotDto` as-is (no UI-layer math for totals/availability).
- UI actions must be performed exclusively through `IBudgetEngine`.
- Engine errors must be mapped from stable error codes (not exception strings).

References:
- Phase 3 checklist: [PHASE3_CHECKLIST.md](PHASE3_CHECKLIST.md)
- Engine error codes: [ENGINE_ERROR_CODES.md](ENGINE_ERROR_CODES.md)

---

## Research Summary

### Market Analysis

Based on research from [NerdWallet](https://www.nerdwallet.com/finance/learn/best-budget-apps), [CNBC Select](https://www.cnbc.com/select/best-budgeting-apps/), and [Actual Budget](https://actualbudget.org/):

**Key Insights:**
- Envelope budgeting apps like Goodbudget and Actual Budget have strong followings
- Privacy-focused, local-first apps are gaining traction
- Users want to "only budget cash on hand" - realistic budgeting
- Automated features see 40% higher user retention
- Visual forecasting and calendar-based planning deliver universal value

**Competitive Landscape:**
| App | Strengths | Gaps We Can Fill |
|-----|-----------|------------------|
| Goodbudget | Great for beginners, sync | Web-only, limited customization |
| Actual Budget | Open source, fast | Steeper learning curve |
| YNAB | Feature-rich | Subscription model, cloud-required |
| EveryDollar | Zero-based budgeting | Limited free tier |

### Technology Stack

Based on [Microsoft Learn](https://learn.microsoft.com/en-us/windows/apps/winui/winui3/) and [Windows Developer Blog](https://blogs.windows.com/windowsdeveloper/2024/11/07/so-whats-new-with-microsoft-native-ux-technologies/):

**WinUI 3 + Windows App SDK:**
- Modern Fluent Design (Mica, Acrylic effects)
- DirectX 12 rendering for smooth 60fps UI
- Native AOT support for fast startup
- Modular metapackage structure (use only what we need)
- Runs on Windows 10 1809+ and Windows 11

**SQLite for Local Data:**
Per [SQLite.org](https://sqlite.org/whentouse.html):
- 35% faster than traditional databases for read/write
- Zero configuration, self-contained
- Perfect for < 1TB local storage
- Parameterized queries prevent SQL injection

---

## Architecture

### Layer Diagram

```
┌─────────────────────────────────────────────────────────┐
│                    Presentation Layer                    │
│                 (WinUI 3 XAML + ViewModels)             │
├─────────────────────────────────────────────────────────┤
│                    Application Layer                     │
│            (Services, Use Cases, Validation)            │
├─────────────────────────────────────────────────────────┤
│                      Domain Layer                        │
│         (Entities, Value Objects, Domain Logic)         │
├─────────────────────────────────────────────────────────┤
│                   Infrastructure Layer                   │
│           (SQLite, Repositories, File I/O)              │
└─────────────────────────────────────────────────────────┘
```

### Phase 1 Focus: Deterministic Core (No UI)

Phase 1 builds the foundational layers that don't change based on UI decisions:

1. **Domain Models** - The nouns of our system
2. **Data Access** - How we persist and retrieve
3. **Business Logic** - The rules that govern behavior
4. **Validation** - Ensuring data integrity
5. **Tests** - Proving correctness

---

## Domain Model

### Core Entities

```
Account
├── Id (Guid)
├── Name (string)
├── Type (Checking, Savings, CreditCard, Cash)
├── CurrentBalance (decimal)
├── IsActive (bool)
├── CreatedAt (DateTime)
└── LastUpdatedAt (DateTime)

Envelope (Budget Category)
├── Id (Guid)
├── Name (string)
├── AllocatedAmount (decimal)     ← How much budgeted this period
├── SpentAmount (decimal)         ← Calculated from transactions
├── Available (decimal)           ← Allocated - Spent
├── Color (string)                ← For UI theming
├── SortOrder (int)
├── IsActive (bool)
└── GroupName (string)            ← Optional grouping

Transaction
├── Id (Guid)
├── AccountId (Guid)
├── EnvelopeId (Guid?)            ← Null = unassigned
├── Amount (decimal)              ← Negative = outflow
├── Date (DateOnly)
├── Payee (string)
├── Memo (string?)
├── IsCleared (bool)
├── IsReconciled (bool)
└── CreatedAt (DateTime)

BudgetPeriod
├── Id (Guid)
├── Year (int)
├── Month (int)
├── TotalIncome (decimal)
├── TotalAllocated (decimal)
├── TotalSpent (decimal)
└── ReadyToAssign (decimal)       ← Income not yet in envelopes

EnvelopeAllocation
├── Id (Guid)
├── EnvelopeId (Guid)
├── BudgetPeriodId (Guid)
├── AllocatedAmount (decimal)
└── RolloverFromPrevious (decimal)
```

### Value Objects

```
Money
├── Amount (decimal)
├── Currency (string) = "USD"

DateRange
├── Start (DateOnly)
├── End (DateOnly)
```

---

## Key Business Rules

### Envelope Budgeting Rules

1. **Only Budget Real Money**: You can only allocate money you actually have
2. **Every Dollar Has a Job**: Income should be assigned to envelopes
3. **Roll With the Punches**: Move money between envelopes as needed
4. **Age Your Money**: Track how long money sits before spending

### Transaction Rules

1. Transactions must have an account
2. Outflows should be assigned to an envelope (warn if not)
3. Transfers between accounts are zero-sum
4. Reconciliation locks transactions from editing

### Balance Calculation

```
Account.CurrentBalance =
    Sum(cleared_transactions) + Sum(uncleared_transactions)

Envelope.Available =
    Sum(allocations_for_period)
    + Rollover_from_previous
    - Sum(transactions_in_envelope)

ReadyToAssign =
    Total_Income - Sum(all_envelope_allocations)
```

---

## Project Structure

```
NextLedger/
├── src/
│   ├── NextLedger.Domain/           ← Entities, Value Objects
│   ├── NextLedger.Application/      ← Services, Use Cases
│   ├── NextLedger.Infrastructure/   ← SQLite, Repositories
│   └── NextLedger.App/              ← WinUI 3 (Phase 2+)
├── tests/
│   ├── NextLedger.Domain.Tests/
│   ├── NextLedger.Application.Tests/
│   └── NextLedger.Infrastructure.Tests/
├── DESIGN.md
├── README.md
└── NextLedger.sln
```

---

## Phase 1 Commits (Deterministic Core)

| # | Commit | Description |
|---|--------|-------------|
| 1 | Initial scaffold | Solution, projects, .gitignore |
| 2 | Domain models | Account, Transaction, Envelope, etc. |
| 3 | Database layer | SQLite setup, migrations, repositories |
| 4 | Envelope service | Allocation, rollover, available balance |
| 5 | Transaction service | CRUD, categorization, splits |
| 6 | Account service | Balance tracking, reconciliation |
| 7 | Budget engine | Period calculations, ready-to-assign |
| 8 | Validation layer | FluentValidation rules |
| 9 | Unit tests | Domain and service tests |
| 10 | Integration tests | Repository and database tests |

---

## UX Principles (For Future Phases)

Even though Phase 1 is backend-only, we design with UX in mind:

1. **Instant Feedback**: Every action confirms success/failure
2. **Undo Everything**: No destructive actions without recovery
3. **Keyboard First**: Power users shouldn't need a mouse
4. **Progressive Disclosure**: Simple by default, powerful when needed
5. **Dark Mode Native**: Built-in, not bolted on

### UI Inspirations

- **Actual Budget**: Clean, fast, focused
- **Windows 11 Settings**: Modern Fluent Design
- **Obsidian**: Keyboard-centric, power-user friendly

---

## Success Metrics

### Phase 1 Complete When:

- [ ] All domain entities have comprehensive tests
- [ ] Repository pattern fully implemented
- [ ] Envelope allocation logic handles edge cases
- [ ] Transaction categorization works correctly
- [ ] Balance calculations are proven accurate
- [ ] Integration tests pass with real SQLite database

---

## Future Phases Preview

**Phase 2**: Basic WinUI 3 shell, navigation, account list
**Phase 3**: Transaction entry, envelope management UI
**Phase 4**: Budget view, spending visualization
**Phase 5**: Reports, trends, insights
**Phase 6**: Import/export (OFX, CSV, QFX)
**Phase 7**: Recurring transactions, scheduling
**Phase 8**: Goals, debt paydown tracking

---

## References

- [WinUI 3 Documentation](https://learn.microsoft.com/en-us/windows/apps/winui/winui3/)
- [Windows App SDK](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/)
- [SQLite in Windows Apps](https://learn.microsoft.com/en-us/windows/apps/develop/data-access/sqlite-data-access)
- [Actual Budget](https://actualbudget.org/) - Inspiration
- [Envelope Budgeting Method](https://www.nerdwallet.com/finance/learn/best-budget-apps)
