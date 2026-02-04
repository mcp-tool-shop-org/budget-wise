# NextLedger — Future Ledger Capability Vision

> A future ledger is an authoritative system of financial truth that can observe, reason about, propose, validate, and execute state changes — with explicit human agency at every boundary.

---

## I. Core Ledger Responsibilities (Non-Negotiable)

These are the minimum invariants. If any are violated, trust collapses.

### 1. Single Source of Truth
- Canonical balances are derived deterministically
- All derived values can be recomputed
- No hidden state
- No "best guess" math

### 2. Reconciliation First
- Every external system is reconcilable
- Differences are surfaced, not masked
- "Matches reality" is a first-class success state

### 3. Explicit Causality
Every state change answers:
- What changed?
- Why did it change?
- Who approved it?
- When did it happen?

**No silent automation.**

---

## II. Authority Model (The Key Distinction)

Future ledgers are **authoritative**, not **autonomous**.

### Authority means the ledger can:
- Validate whether an action should occur
- Enforce constraints before execution
- Refuse actions that violate rules
- Produce a verifiable record of intent

### Authority does NOT mean:
- Acting on behalf of the user without consent
- Holding keys by default
- Optimizing behind the scenes

---

## III. The Five Capability Layers

These layers are additive, not mutually exclusive.

### Layer 1 — Observation (v1 baseline)

*The ledger can see.*

**Capabilities:**
- Observe balances (fiat, crypto, external)
- Observe transactions
- Observe protocol state
- Observe discrepancies

**Constraints:**
- No mutation
- No signing
- No execution

*This is where trust is established.*

### Layer 2 — Interpretation

*The ledger can understand.*

**Capabilities:**
- Classify transactions
- Normalize data across systems
- Detect anomalies
- Explain outcomes in human language

**Examples:**
- "This envelope is overspent because…"
- "This balance differs from your bank because…"

*This is where usability is won.*

### Layer 3 — Intent Declaration (Critical Transition)

*The ledger can capture intent.*

**Capabilities:**
- User declares what they want to happen
- Ledger validates feasibility
- Ledger simulates outcomes
- Ledger highlights conflicts or risks

**Examples:**
- "I want to move $500 to Rent"
- "I want to rebalance assets"
- "I want to fund this goal before anything else"

**Important:** Intent ≠ execution.

*This is where future ledgers diverge from dashboards.*

### Layer 4 — Constraint Enforcement

*The ledger can say no.*

**Capabilities:**
- Budget constraints
- Risk constraints
- Temporal constraints
- Policy constraints (user-defined)

**Examples:**
- "This would violate your budget rules"
- "This would increase exposure beyond your limit"
- "This action is blocked until reconciliation"

*This is where ledgers become protective, not permissive.*

### Layer 5 — User-Approved Execution

*The ledger can finalize change, but never alone.*

**Capabilities:**
- Prepare execution payloads
- Require explicit user approval
- Delegate signing to external agents:
  - Hardware wallet
  - OS wallet
  - Bank auth
- Verify execution success

**Key rule:** The ledger coordinates execution — it does not own it.

*This is where future ledgers touch the chain safely.*

---

## IV. Execution Boundaries (Hard Rules)

A future ledger must enforce these boundaries:

### Never:
- Hold private keys by default
- Execute without explicit approval
- Auto-retry irreversible actions
- Obscure gas/fees/risk
- Bundle multiple intents silently

### Always:
- Show preview before execution
- Show post-execution verification
- Preserve an immutable audit trail
- Allow "do nothing" safely

---

## V. Human Experience Guarantees

Future ledgers succeed or fail here.

### Psychological Safety
- No shame language
- No panic states
- Errors are explanations, not accusations

### Cognitive Safety
- One decision at a time
- No hidden coupling
- Clear "what happens next"

### Temporal Safety
- Past is immutable
- Present is inspectable
- Future is previewable

---

## VI. Programmable, Not Automated

This is subtle but critical.

### Automation is:
- "Do X when Y happens"
- Often invisible
- Often irreversible

### Programmable constraints are:
- Rules of truth
- Always inspectable
- Always overrideable
- Always attributable

**Future ledgers enforce rules, not behaviors.**

---

## VII. Ledger ≠ Wallet ≠ App

Clear separation of roles:

| Component | Responsibility |
|-----------|----------------|
| **Ledger** | Truth, validation, audit |
| **Wallet** | Signing, custody |
| **App** | Interaction, visualization |

NextLedger should always remain **ledger-first**.

---

## VIII. What a Future Ledger Enables (Safely)

Because of this structure, you can safely support:

- Multi-asset planning
- On-chain visibility
- DeFi position tracking
- Intent-based financial actions
- Cross-system reconciliation
- Verifiable personal finance history

**Without:**
- Custody risk
- Regulatory landmines
- "Oops, funds lost" moments

---

## IX. The North-Star Invariant

If this invariant holds, you're building the right thing:

> **Nothing changes unless a human can see it, understand it, approve it, and verify it.**

That sentence should be true at every phase.

---

## X. External Messaging

### Never say:
- "Read-only forever"
- "Trustless magic"
- "One-click finance"

### Instead say:
- "User-approved"
- "Non-custodial"
- "Explicit and verifiable"
- "Nothing happens behind your back"

---

## Current Status

| Layer | Status | Notes |
|-------|--------|-------|
| Layer 1 — Observation | ✅ Complete | Local SQLite, account tracking, transactions |
| Layer 2 — Interpretation | ✅ Complete | Envelope budgeting, spending analysis |
| Layer 3 — Intent Declaration | 🔜 Planned | Budget allocations are early intent |
| Layer 4 — Constraint Enforcement | 🔜 Planned | Overspend warnings exist |
| Layer 5 — User-Approved Execution | 🔮 Future | Web3 integration roadmap |

---

*Document created: 2025*
*NextLedger v1.0.0*
