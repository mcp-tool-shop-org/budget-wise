# NextLedger — Future Ledger Execution Checklist

> **Purpose:** Turn the [FUTURE_LEDGER_VISION.md](FUTURE_LEDGER_VISION.md) into an enforceable, lived architecture
> **Scope:** Product · Architecture · UX · Safety · Governance
> **Rule:** Nothing on this list is optional if you want to claim "future ledger" honestly.

---

## 0. Vision Enforcement Gate (Run Before Any Feature)

Before adding any new capability, confirm:

- [ ] Feature maps to one of the 5 capability layers
- [ ] Feature does not violate execution boundaries
- [ ] Feature preserves human approval
- [ ] Feature keeps Ledger ≠ Wallet ≠ App intact
- [ ] Feature can be explained in one sentence to a non-technical user

**If any answer is ❌ → stop.**

---

## 1. Capability Layer 1 — Observation (Must Be Perfect)

*The ledger sees reality clearly.*

### Data Intake
- [ ] All external data sources are explicitly labeled (Bank, CSV, Chain, Manual)
- [ ] Source metadata stored with every transaction/balance
- [ ] No silent background ingestion
- [ ] Failures are visible and diagnosable

### Determinism
- [ ] Every observed balance can be recomputed
- [ ] Recalculation is idempotent
- [ ] Observed ≠ derived is clearly separated

### UX Proof
- [ ] User can answer: "Where did this number come from?"
- [ ] User can see when data is stale

---

## 2. Capability Layer 2 — Interpretation (Human Clarity)

*The ledger explains reality.*

### Explanation Engine
- [ ] Every blocked action has a human-readable reason
- [ ] Every discrepancy has an explanation path
- [ ] Overspending, drift, mismatch are normalized, not alarmist

### Language Audit
- [ ] No "invalid", "exception", "failed"
- [ ] Uses "needs attention", "can't do yet", "here's why"
- [ ] No jargon without tooltip explanation

### UX Proof
- [ ] A confused user can recover without Help tab
- [ ] Help tab deepens understanding, doesn't rescue basics

---

## 3. Capability Layer 3 — Intent Declaration (Critical Shift)

*The ledger understands what the user wants to happen.*

### Intent Model
- [ ] Intent is a first-class domain object
- [ ] Intent captures:
  - [ ] What
  - [ ] Why
  - [ ] Scope
  - [ ] Preconditions
- [ ] Intent is stored before execution

### UI Requirements
- [ ] User explicitly declares intent (never inferred)
- [ ] Intent preview shows:
  - [ ] Before state
  - [ ] After state
  - [ ] Side effects
- [ ] User can cancel intent safely

### UX Proof
- [ ] User never asks "what just happened?"
- [ ] User always knows "what I'm about to do"

---

## 4. Capability Layer 4 — Constraint Enforcement (Protection)

*The ledger can say "no" — and explain why.*

### Constraint System
- [ ] Budget constraints enforced centrally
- [ ] Temporal constraints (reconciliation, period locks)
- [ ] User-defined rules are inspectable
- [ ] Constraints are evaluated before execution

### Failure UX
- [ ] Constraint failures explain:
  - [ ] Which rule
  - [ ] Why it exists
  - [ ] How to proceed
- [ ] No dead ends

### UX Proof
- [ ] Being blocked feels protective, not punitive

---

## 5. Capability Layer 5 — Execution (Only When Earned)

*The ledger coordinates change, but never owns it.*

### Hard Execution Boundaries
- [ ] No private key storage
- [ ] No silent execution
- [ ] No auto-retries on irreversible actions
- [ ] No bundled actions without explicit review

### Approval Flow
- [ ] Execution requires explicit user approval
- [ ] Signing is delegated:
  - [ ] Hardware wallet
  - [ ] OS wallet
  - [ ] External provider
- [ ] Ledger verifies execution result

### Audit Trail
- [ ] Intent → Approval → Execution → Verification is persisted
- [ ] Immutable history viewable by user

---

## 6. Ledger ≠ Wallet ≠ App (Enforced Separation)

### Architecture
- [ ] Ledger has no signing code
- [ ] Wallet integration is via adapter interfaces only
- [ ] App UI cannot bypass ledger validation

### UX Framing
- [ ] Users understand:
  - [ ] Ledger = truth
  - [ ] Wallet = signer
  - [ ] App = interface
- [ ] No "Connect Wallet" surprise flows

---

## 7. Human Experience Guarantees (Non-Negotiable)

### Emotional Safety
- [ ] Overspending is normalized
- [ ] Errors are calm
- [ ] Success is acknowledged

### Cognitive Safety
- [ ] One decision per screen
- [ ] No compound actions
- [ ] Always-visible "do nothing" path

### Trust Signals
- [ ] Diagnostics explain themselves
- [ ] Privacy boundaries stated clearly
- [ ] No hidden automation

---

## 8. Governance & Scope Guardrails

### Explicit "Will Never Do" List
- [ ] Hold private keys by default
- [ ] Auto-execute financial actions
- [ ] Promise yields or profits
- [ ] Hide execution complexity

### Review Discipline
- [ ] Any execution feature requires:
  - [ ] Vision alignment check
  - [ ] Threat modeling
  - [ ] UX walkthrough
- [ ] One-way doors are called out explicitly

---

## 9. Documentation Alignment Check

For every new feature:

- [ ] [FUTURE_LEDGER_VISION.md](FUTURE_LEDGER_VISION.md) still reads as true
- [ ] README.md roadmap remains accurate
- [ ] Help content updated if behavior changed
- [ ] Terminology consistent ("intent", "approval", "execution")

**If docs lie → feature is incomplete.**

---

## 10. The North-Star Invariant (Final Gate)

Before shipping anything touching Web3 or execution, confirm:

> **Nothing changes unless a human can see it, understand it, approve it, and verify it.**

- [ ] **See it** — Change is visible before it happens
- [ ] **Understand it** — Explanation is human-readable
- [ ] **Approve it** — Explicit user consent required
- [ ] **Verify it** — Post-execution confirmation provided

**If any box is ❌ → do not ship.**

---

## How to Use This Checklist

1. **Treat it as a pull request rubric** — Every PR touching core functionality should reference relevant sections
2. **Reference it in PR templates** — Add a checkbox: "Reviewed against LEDGER_EXECUTION_CHECKLIST.md"
3. **Say "no" when something violates it** — Even if it's exciting
4. **Update it when you learn** — This is a living document

---

## Quick Reference: Layer → Status

| Layer | Current Status | Gate |
|-------|---------------|------|
| 1. Observation | ✅ v1.0 | Production |
| 2. Interpretation | ✅ v1.0 | Production |
| 3. Intent Declaration | 🔜 Planned | Design required |
| 4. Constraint Enforcement | 🔜 Planned | Design required |
| 5. User-Approved Execution | 🔮 Future | Full checklist required |

---

*Last updated: 2025*
*Companion to: [FUTURE_LEDGER_VISION.md](FUTURE_LEDGER_VISION.md)*
