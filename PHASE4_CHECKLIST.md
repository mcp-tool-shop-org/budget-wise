# Phase 4 Checklist — Transactions, Import, Reconciliation

## Phase Goal
Make transaction handling production-grade: correct accounting behavior, robust import, predictable reconciliation, and UI workflows that stay thin over `IBudgetEngine`.

## Exit Criteria
A user can import or enter transactions, categorize them (including splits), reconcile accounts, and trust that budgets update deterministically via engine recalculation—with full test coverage and no drift.

---

## 1) Engine Contract Expansion (If Needed)

Confirm engine supports all transaction operations required by UI:

- Add transaction (inflow/outflow)
- Edit transaction
- Delete transaction (soft delete recommended)
- Assign / change envelope category (including “Unassigned”)
- Transfers (account ↔ account)

Ensure all operations return `BudgetOperationResult` with:

- Snapshot
- Change records (allocation/transaction changes)
- Structured errors

- Document and freeze transaction-related error codes

**Definition of Done:** UI never calls repositories directly; all writes go through the engine.

---

## 2) Transaction Domain Rules (Correctness First)

Formalize transaction types and invariants:

- Inflow vs outflow vs transfer
- Transfer must balance (one outflow + one inflow)
- Envelope assignment rules (transfers usually uncategorized)

Define lifecycle flags (at minimum):

- Cleared vs uncleared
- Reconciled vs unreconciled
- Optional: pending

Ensure “unassigned spending” is first-class and doesn’t break totals.

**Definition of Done:** Transactions behave consistently across UI, import, and reconciliation.

---

## 3) Splits (If in Phase 4 Scope)

Add split transaction model:

- One parent transaction
- N split lines with envelope + amount
- Parent amount must equal sum(splits)
- Recalc uses split lines for envelope spending totals
- UI supports basic split editor (even minimal)

**Definition of Done:** Splits work in engine + persistence + UI without special-case math.

---

## 4) Import Pipeline (CSV First, OFX/QFX Later)

Define import format and workflow:

- CSV import (bank export compatible)
- Column mapping UI (optional; can be config-driven first)

Normalize imported data:

- Date parsing
- Amount sign conventions (bank formats vary)
- Payee normalization
- Memo/notes handling

Duplicate detection (minimum viable):

- Hash key: (date, amount, payee, memo?) with tolerance rules

Import preview showing duplicates/skips.

Import results return structured summary:

- Inserted / skipped / flagged counts

**Definition of Done:** User can import a CSV and safely avoid duplicate spam.

---

## 5) Payee Rules & Auto-Categorization (Optional but High Leverage)

Create rules engine MVP:

- If payee contains X → set envelope Y
- Optional: rename payee to canonical

Apply rules during import and manual entry.

Rules are deterministic + test-covered.

UI supports creating/editing rules minimally.

**Definition of Done:** Imports require less manual cleanup over time.

---

## 6) Reconciliation (Must-Have for Trust)

Add reconciliation workflow:

- Input statement ending balance + date
- Mark cleared transactions
- Compute difference (target vs current)
- When zero, mark selected as reconciled

Handle adjustment transaction (optional but common):

- If difference ≠ 0 → create adjustment (explicit)

Ensure reconciliation doesn’t mutate budget math incorrectly:

- Reconcile is a status change, not a money change

**Definition of Done:** Users can reconcile accounts without budget totals drifting.

---

## 7) UI Flows (Thin Client Over Engine)

Transactions list view:

- Filters: month, account, cleared status
- Sort by date, amount

Transaction editor:

- Create/edit/delete
- Assign envelope (or split)
- Toggle cleared

Import view:

- File picker
- Preview grid
- Duplicate warnings
- Commit import

**Definition of Done:** UI calls engine methods only; no inline calculations.

---

## 8) Persistence & Query Performance

Add indexes for common queries:

- (AccountId, Date)
- EnvelopeId
- Cleared/Reconciled flags
- Transfer link keys

Ensure migration strategy remains stable (Phase 3/4).

Confirm SQLite FK behavior continues to pass integration tests.

**Definition of Done:** Imports and listing transactions remain responsive as data grows.

---

## 9) Testing & Acceptance Gates (Non-Negotiable)

Expand tests across layers:

- Domain invariants (splits, transfers)
- Application engine orchestration tests
- SQLite integration import tests

End-to-end acceptance scenarios:

- Import → auto-categorize → recalc → budgets updated
- Transfer → correct link + no envelope spend impact
- Reconciliation → cleared/reconciled flags behave + no total drift
- Split transaction → envelope spends correct + edits persist

Final Gate: `dotnet test -c Release` ✅ and acceptance scenarios validated.

---

## Phase 4 Deliverables Summary (Stakeholder-Friendly)

- Production-grade transaction workflows (manual + import)
- Duplicate-safe CSV import with preview
- Optional payee rules for auto-categorization
- Reconciliation with cleared/reconciled tracking
- Full test coverage across engine + SQLite integration + UI

---

## Status (as of 2026-02-03)

- **Completed**: transaction soft-delete, transfers, cleared toggle, reconciliation (engine + integration tests), CSV import preview/commit (engine + UI), and instrumentation.
- **Completed**: import acceptance coverage (preview detects duplicates/invalid rows; commit is idempotent and recalculates) via Infrastructure integration tests.
- **Completed**: query performance indexes for common filters, including composite `(AccountId, Date)` and reconciled flag index.

### Deferred (Explicitly Out of Phase 4)

- **Splits**: completed end-to-end (domain invariants + persistence + recalc + UI editor).
- **Payee rules / auto-categorization**: optional; deferred until after core import/reconcile workflows are stable.
