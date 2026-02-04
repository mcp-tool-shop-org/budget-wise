# Phase 3 Checklist — UI Foundation & Engine Integration

## Phase Goal
Expose the Phase 2 Budget Engine through a stable, user-facing UI layer with clear contracts, predictable performance, and UX flows that match real budgeting behavior.

## Exit Criteria
A functional budgeting UI backed exclusively by `IBudgetEngine`, with frozen contracts, verified UX flows, and no business logic leakage into the presentation layer.

---

## 1) Contract Freeze & Guardrails (Must-Do First)
- Freeze `BudgetSnapshotDto` as the read contract for UI.
- Freeze `BudgetOperationResult` + `BudgetOperationError` codes.
- Document all engine error codes with:
  - User-facing message
  - Recovery action (if any)
  - Catalog: [ENGINE_ERROR_CODES.md](ENGINE_ERROR_CODES.md)
- Add contract regression tests to prevent breaking changes.
- Prohibit UI-layer math (`ReadyToAssign`, `Available`, etc.).

**Definition of Done:** UI consumes engine data as-is; no derived totals or duplicated rules.

---

## 2) Performance Posture Decision (Explicit)
- Confirm recalculation strategy:
  - Recalc after every write (current default), or
  - Incremental updates + periodic reconcile (future)
- Define acceptable latency budget for:
  - Allocation changes
  - Move operations
  - Transaction entry
- Add lightweight timing instrumentation around engine calls.

**Definition of Done:** Performance expectations are documented and testable before UI optimization begins.

---

## 3) Application State Model (UI Backbone)
- Define `BudgetViewState` (single source of truth for the screen).
- Map engine snapshot → view state explicitly.
- Identify transient UI state vs persisted domain state.
- Ensure all state transitions originate from engine results.

**Definition of Done:** UI state updates only via `IBudgetEngine` responses, never speculative updates.

---

## 4) Budget Screen (Core Experience)
- Display:
  - Ready-to-Assign
  - Envelope list with Available / Spent / Goal status
  - Inline allocation editing (increase/decrease)
  - Visual overspending indicators (negative Available)
- Optimistic UI disabled until engine response succeeds.
- Error states mapped from engine error codes.

**Definition of Done:** Users can allocate, deallocate, and move money without violating engine rules.

---

## 5) Envelope Detail & Goals
- Envelope detail view shows:
  - Allocation
  - Spent
  - Rollover
  - Goal (if present)
- Goal creation/edit flow wired to `SetGoal`.
- Goal progress visualization (no new math).
- Auto-assign trigger exposed (manual action).

**Definition of Done:** Goals are fully engine-backed; UI never infers funding status independently.

---

## 6) Transaction Entry (Minimal but Correct)
- Manual transaction entry:
  - Amount
  - Date
  - Payee
  - Envelope (optional)
- Support unassigned spending.
- Transfer entry path uses engine-safe flow.
- Validation errors surfaced from engine results.

**Stretch (optional Phase 3.5):** Split transactions.

**Definition of Done:** Transactions update budgets only via engine-triggered recalculation.

---

## 7) Navigation & Period Switching
- Month selector (previous / next / jump).
- Read-only behavior for closed periods.
- Explicit rollover action surfaced.
- Clear visual distinction between open vs closed periods.

**Definition of Done:** Users understand when a month is mutable vs finalized.

---

## 8) Error Handling & UX Clarity
- Standard error display component.
- Friendly messaging mapped to engine error codes.
- No raw exceptions leak to UI.
- Retry affordances where appropriate.

**Definition of Done:** Every failure path has a predictable, user-comprehensible outcome.

---

## 9) Data Safety & Confidence Signals
- Visual confirmation after successful operations.
- Disable destructive actions during engine calls.
- Add backup reminder or placeholder (no implementation yet).
- Clear indicators for recalculation completion.

**Definition of Done:** Users trust that what they see reflects persisted, recalculated truth.

---

## 10) Phase 3 Acceptance & Quality Gate
- End-to-end UI + engine scenarios:
  - Allocate → spend → recalc → rollover
  - Overspend → negative rollover display
  - Auto-assign to goals → snapshot update
- No direct repository access from UI.
- All UI tests pass.
- No Phase 2 tests broken.

**Final Gate:** UI is a thin client over `IBudgetEngine`, with zero duplicated business logic.

---

## Phase 3 Deliverables Summary (for planning docs)
- Frozen engine → UI contract
- Working budget screen
- Engine-backed allocation, moves, and transactions
- Predictable error handling
- Clear month lifecycle UX
