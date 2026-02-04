# Phase 5 Checklist — UX Polish, Performance Hardening & Release Readiness

## Tracking (Run This Like a Release)

Use this table as the single source of truth for Phase 5 execution.

| Area | Priority | Owner | Status | Evidence / Notes |
|---|---|---|---|---|
| 1. UX Polish & Interaction Clarity | High |  | Not Started | Link screenshots / recordings per screen |
| 2. Error Messaging & Trust Signals | High |  | Not Started | Error-to-UX mapping table + before/after UX |
| 3. Performance Posture & Measurement | High |  | Not Started | Baselines captured + accepted thresholds |
| 4. Data Safety & Recovery | High |  | Not Started | Backup/restore steps validated on fresh machine |
| 5. First-Run & Onboarding Experience | High |  | Not Started | Time-to-first-successful-budget < 10 min |
| 6. Configuration & Diagnostics Hardening | High |  | Not Started | “Copy diagnostics” output reviewed for safety |
| 6a. Web3/XRPL Diagnostics Safety | Med |  | Not Started | No secrets leaked; clear status/timeout/errors |
| 7. Stability, CI, and Build Hygiene | High |  | Not Started | Repro + mitigation for WinUI Release flake |
| 8. Documentation & Product Surface | Med |  | Not Started | README + mental-model docs reviewed |
| 9. Release Packaging (Desktop-Ready) | High |  | Not Started | Artifact can be handed to a tester |
| 10. Phase 5 Acceptance Gate | High |  | Not Started | Release tests + smoke test checklist completed |

## Phase Goal
Turn a technically complete budgeting application into a trustworthy, performant, and release-ready product. Improve UX clarity, eliminate sharp edges, harden data safety, and prepare for first external users.

## Exit Criteria
The app feels stable, predictable, and safe to use daily. UX communicates system state clearly. Performance is measured and acceptable. Release artifacts are ready.

---

## 1. UX Polish & Interaction Clarity (High Impact)

**Budget screen refinements**

- Clear visual hierarchy for Ready-to-Assign
- Strong negative/overspent affordances (color + iconography)
- Consistent formatting for currency and dates

**Envelope list UX**

- Inline indicators for goals, overspending, zero-available
- Hover / selection states that expose detail without navigation

**Split transaction UX polish**

- Inline sum indicator (split total vs transaction amount)
- Validation hints for mismatched totals
- Clear visual distinction between parent + split lines

**Transaction list readability**

- Icons or badges for transfers, splits, reconciled items
- Optional compact vs detailed view toggle

**Definition of Done:** Users can understand what’s happening at a glance without reading documentation.

---

## 2. Error Messaging & Trust Signals

- Finalize error-to-UX mapping table:
  - Friendly, human-readable messages for all engine error codes
  - Clear guidance on how to resolve common failures
- Add non-intrusive success confirmations:
  - Allocation changes
  - Import commits
  - Reconciliation completion
- Prevent silent failures:
  - No swallowed exceptions
  - No “nothing happened” states

**Definition of Done:** Every action produces a clear, confidence-building outcome.

---

## 3. Performance Posture & Measurement

- Add lightweight instrumentation around:
  - Engine calls (allocation, move, import commit)
  - Recalculation duration
  - Transaction list load times
- If Web3/XRPL is enabled, measure:
  - Diagnostics call latency (server_info, account_info)
  - Timeout/error rates (friendly surfaced errors)
- Establish performance baselines:
  - Cold start
  - Month with ~5k transactions
  - CSV import of 1k+ rows
- Decide optimization strategy:
  - Keep full recalculation-after-write (acceptable), or
  - Plan incremental updates for Phase 6+

**Definition of Done:** Performance is measured, understood, and explicitly accepted.

---

## 4. Data Safety & Recovery

- Manual backup/export workflow:
  - Export SQLite DB or structured backup file
  - Clear user-facing backup instructions
- Restore workflow:
  - Restore from backup
  - Validation + error handling
- Guard against accidental destructive actions:
  - Confirmation dialogs for deletes
  - Undo affordances where feasible (soft delete window)

**Definition of Done:** Users can recover from mistakes and trust their data is not fragile.

---

## 5. First-Run & Onboarding Experience

- First-run detection:
  - Empty state UX (no accounts / no envelopes)
- Guided setup (minimal):
  - Create first account
  - Create first envelope
  - Add first transaction
- Optional sample data toggle for exploration
- Tooltips or microcopy for core concepts:
  - Ready-to-Assign
  - Overspending
  - Rollover

**Definition of Done:** A new user can reach “I get it” in under 10 minutes.

---

## 6. Configuration & Diagnostics Hardening

- Surface key configuration states in Diagnostics:
  - Database path
  - App version
  - Web3/XRPL status (if enabled)
- Add “Copy diagnostics” for support/debugging
- Ensure diagnostics are safe (no secrets leaked)

Web3/XRPL notes (keep support-safe and predictable):

- Never include secrets in copied diagnostics.
- Display Web3/XRPL status clearly when enabled/disabled.
- Ensure requests have timeouts and produce user-actionable errors.

**Definition of Done:** Issues can be diagnosed without attaching a debugger.

---

## 7. Stability, CI, and Build Hygiene

- Investigate WinUI/XAML Release build flakiness:
  - Repro steps documented
  - Mitigation or CI workaround applied
- Clean CI pipeline:
  - Fresh restore
  - Build
  - Test
- Zero warnings policy for Release builds (or explicitly documented exceptions)

**Definition of Done:** Builds are boring, repeatable, and low-friction.

---

## 8. Documentation & Product Surface

- README updated:
  - What BudgetWise is (and is not)
  - Key principles (offline-first, deterministic, no drift)
- Short “How budgeting works here” doc:
  - Ready-to-Assign
  - Envelope rollover
  - Overspending behavior
- Developer docs:
  - Architecture overview
  - Engine-first rule

**Definition of Done:** Future contributors and users understand the mental model.

---

## 9. Release Packaging (Desktop-Ready)

- Decide packaging model:
  - Unpackaged (zip/installer), or
  - MSIX (if/when desired)
- App identity finalized:
  - Name
  - Icon
  - Versioning scheme
- Version displayed in UI (About / Diagnostics)

**Definition of Done:** You can hand the app to someone else without explanation.

---

## 10. Phase 5 Acceptance Gate

- All Phase 5 checklist items reviewed
- No UI-layer business logic added
- Engine remains single source of truth
- `dotnet test -c Release` passes
- Manual smoke test completed:
  - Budget → transactions → import → reconciliation → rollover → restart app

**Final Gate:** The app is daily-usable, trustworthy, and release-shaped.

---

## Phase 5 Deliverables Summary (Stakeholder-Friendly)

- Polished, confidence-building UX
- Measured and accepted performance posture
- Data safety via backup/restore
- Clear onboarding and documentation
- Release-ready desktop artifact

---

## Recommendation

Phase 5 is where you stop adding features and start earning trust.
