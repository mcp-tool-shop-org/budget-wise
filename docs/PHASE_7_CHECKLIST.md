# NextLedger Phase 7 — Feature Development Checklist

> **Phase Name:** Intent & Insight Expansion
> **Purpose:** Safely add real features that move NextLedger forward
> **Scope:** Layer 2 (Interpretation) → Layer 3 (Intent), no execution yet

---

## Gate A — Feature Eligibility Check (Run First)

Before starting any feature:

- [ ] Feature clearly maps to Layer 2 (Interpretation) or Layer 3 (Intent)
- [ ] Feature does not require signing, custody, or execution
- [ ] Feature strengthens clarity, control, or confidence
- [ ] Feature can be explained in one sentence to a non-technical user
- [ ] Feature does not require weakening any checklist gate

**If any ❌ → stop or redesign.**

---

## 1. Layer 2 Expansion — Insight & Clarity Features

*These make NextLedger smarter without making it dangerous.*

### 1.1 Account & Envelope Management (High ROI)

**Feature:**
- [ ] Add / rename / archive accounts
- [ ] Add / rename / archive envelopes
- [ ] Safe deletion rules (no silent data loss)

**Checklist:**
- [ ] Deletion requires confirmation
- [ ] Archived ≠ deleted (recoverable)
- [ ] Ledger history remains intact
- [ ] UI explains impact clearly

**Why now:**
- Removes known limitation
- Improves daily usability
- Zero execution risk

---

### 1.2 Net Worth View (Fiat-First)

**Feature:**
- [ ] Aggregated view of:
  - [ ] Assets
  - [ ] Liabilities
  - [ ] Net worth over time

**Checklist:**
- [ ] Read-only aggregation
- [ ] Historical snapshots deterministic
- [ ] Clear "what's included" explanation
- [ ] No implied forecasting

**Why now:**
- Reinforces "ledger" concept
- Prepares mental model for multi-asset future

---

### 1.3 Enhanced Explanations ("Why" Everywhere)

**Feature:**
- [ ] Inline explanations for:
  - [ ] Disabled buttons
  - [ ] Blocked actions
  - [ ] Constraint failures

**Checklist:**
- [ ] Every block has a human explanation
- [ ] Explanation includes a next step
- [ ] No Help-tab dependency

**Why now:**
- Reduces cognitive load
- Builds trust in constraints

---

## 2. Layer 3 Introduction — Intent Without Execution

*This is the most important step toward a future ledger.*

### 2.1 Intent Domain Model (Foundational)

**Feature:**
- [ ] Introduce `Intent` as a first-class concept:
  - [ ] `AllocateIntent`
  - [ ] `MoveIntent`
  - [ ] `CoverOverspendIntent`

**Checklist:**
- [ ] Intent stored before action
- [ ] Intent includes:
  - [ ] What
  - [ ] Why
  - [ ] Scope
- [ ] Intent can be cancelled safely
- [ ] No execution coupling

**Why now:**
- Establishes future-proof architecture
- Trains users into intent thinking

---

### 2.2 Intent Preview UI (Before/After)

**Feature:**
- [ ] Show preview panels for:
  - [ ] Allocation
  - [ ] Moves
  - [ ] Overspend coverage

**Checklist:**
- [ ] Before state shown
- [ ] After state shown
- [ ] Side effects highlighted
- [ ] User explicitly confirms intent

**Why now:**
- Reinforces "see → understand → approve"
- Prepares for future execution flows

---

### 2.3 Intent History (Audit Without Action)

**Feature:**
- [ ] View past intents:
  - [ ] Created
  - [ ] Approved
  - [ ] Cancelled

**Checklist:**
- [ ] Immutable history
- [ ] Clear timestamps
- [ ] No implication of execution

**Why now:**
- Builds audit mindset
- Reinforces authority without action

---

## 3. External Ledger (Preview) — Observation Only

*This validates the "Next" in NextLedger.*

### 3.1 Digital Assets (Preview Page)

**Feature:**
- [ ] Add read-only crypto visibility:
  - [ ] Public address input
  - [ ] Balance fetch (XRPL first)

**Checklist:**
- [ ] Explicit "Preview / Read-only" labeling
- [ ] No signing code anywhere
- [ ] Clear privacy explanation
- [ ] Diagnostics for failures

**Why now:**
- Demonstrates future direction
- Zero custody risk

---

### 3.2 External Ledger Framing

**Checklist:**
- [ ] Framed as "Externally reconciled"
- [ ] Not mixed into core budgeting by default
- [ ] Help explains what it can't do

---

## 4. UX & Safety Reinforcement (Cross-Cutting)

These must be rechecked for every new feature:

- [ ] No silent state changes
- [ ] No background execution
- [ ] No compound actions
- [ ] Always a "do nothing" path
- [ ] Diagnostics updated if needed

---

## 5. Documentation & Enforcement Sync

Every feature PR must:

- [ ] Reference the capability layer
- [ ] Pass Gate 0 explicitly
- [ ] Keep [FUTURE_LEDGER_VISION.md](FUTURE_LEDGER_VISION.md) truthful
- [ ] Keep [LEDGER_EXECUTION_CHECKLIST.md](LEDGER_EXECUTION_CHECKLIST.md) unviolated
- [ ] Update Help content if behavior changes

---

## 6. Phase 7 Exit Criteria

You may conclude Phase 7 when:

- [ ] Intent exists as a first-class concept
- [ ] Users regularly see intent previews
- [ ] Net worth and insight views are stable
- [ ] External ledger preview is visible but optional
- [ ] No execution code exists anywhere

At that point, NextLedger is:

> **An authoritative ledger that understands intent — but still never acts alone.**

---

## Progress Tracking

| Feature | Status | Layer | Notes |
|---------|--------|-------|-------|
| Account Management | ⬜ Not Started | L2 | |
| Envelope Management | ⬜ Not Started | L2 | |
| Net Worth View | ⬜ Not Started | L2 | |
| Enhanced Explanations | ⬜ Not Started | L2 | |
| Intent Domain Model | ⬜ Not Started | L3 | |
| Intent Preview UI | ⬜ Not Started | L3 | |
| Intent History | ⬜ Not Started | L3 | |
| Digital Assets Preview | ⬜ Not Started | L1 | XRPL read-only |

---

*Last updated: 2025*
*Depends on: [LEDGER_EXECUTION_CHECKLIST.md](LEDGER_EXECUTION_CHECKLIST.md)*
