# NextLedger — XRPL Feature Checklist (Concrete, Build-Ready)

> **Feature Theme:** XRPL as the first "Next Ledger"
>
> **Ledger Layer Target:**
> - ✅ Layer 1: Observation
> - ✅ Layer 2: Interpretation
> - ⚠️ Layer 3: Intent (non-executing only)
> - ❌ Layer 4–5: Execution (explicitly excluded)

---

## Gate 0 — XRPL Feature Eligibility Check (Required)

Before writing code:

- [ ] XRPL is treated as an external ledger, not a wallet
- [ ] No private keys anywhere in the codebase
- [ ] No signing, submitting, or simulating transactions
- [ ] Feature works with public addresses only
- [ ] Failure modes are explicit and diagnosable

**If any ❌ → stop.**

---

## 1. XRPL Observation Layer (Must Ship First)

*"NextLedger can see XRP, but cannot touch it."*

### 1.1 XRPL Client Hardening

**Checklist:**
- [ ] XRPL client interface isolated behind `IXrplClient`
- [ ] Environment-based RPC configuration
- [ ] Null / disabled client supported (no crash)
- [ ] Timeout + retry policy with clear failure states

**Outcome:** XRPL is a pluggable observer, not a dependency.

---

### 1.2 XRP Account Model

**Feature:** Add a new account type: `ExternalLedgerAccount`
- Subtype: XRPL

**Checklist:**
- [ ] Stores:
  - [ ] Public address
  - [ ] Network (mainnet/testnet)
  - [ ] Last sync timestamp
- [ ] Marked explicitly as "Externally Reconciled"
- [ ] Read-only flag enforced in UI and engine

---

### 1.3 Balance Fetch (Authoritative but External)

**Checklist:**
- [ ] Fetch XRP balance via `account_info`
- [ ] Display:
  - [ ] Raw XRP balance
  - [ ] Network status
- [ ] Surface failures clearly ("Unable to reach XRPL")

**UX Requirement:**
- [ ] Shown alongside fiat accounts
- [ ] Labeled: "Externally reconciled (XRPL)"

---

## 2. XRPL Interpretation Layer (This Is Where You Win)

*Seeing data is table stakes. Explaining it is the product.*

### 2.1 XRP Balance Explanation

**Checklist:**
- [ ] Tooltip explaining:
  - [ ] XRP reserve requirement
  - [ ] Why balance ≠ spendable balance
- [ ] Clear distinction between:
  - [ ] Total balance
  - [ ] Reserved XRP

*This is huge trust value.*

---

### 2.2 XRPL Transaction History (Read-Only Import)

**Feature:** Optional transaction import from XRPL

**Checklist:**
- [ ] Pull transactions via public APIs
- [ ] Normalize into internal Transaction model
- [ ] Mark source as XRPL
- [ ] No mutation of on-chain history
- [ ] Import preview before commit

**UX Rule:**
- [ ] XRPL txns are never editable
- [ ] Clearly labeled "On-chain"

---

### 2.3 XRPL ↔ Ledger Reconciliation

**Feature:** Reconciliation is informational, not corrective

**Checklist:**
- [ ] Show:
  - [ ] Ledger balance
  - [ ] XRPL-reported balance
  - [ ] Difference (if any)
- [ ] Clear messaging: "This ledger reflects the XRPL state as of \<timestamp\>"

**Important:**
- ❌ No auto-adjustments
- ❌ No "fix" buttons

---

## 3. XRPL Intent Layer (Non-Executing, Critical)

*This is where "future ledger" becomes real — without sending funds.*

### 3.1 Intent: Observe / Plan / Explain

**Allowed intents:**
- [ ] "Track this XRPL address"
- [ ] "Explain recent balance change"
- [ ] "Plan allocation using XRP balance (read-only)"

**Checklist:**
- [ ] Intents are stored
- [ ] Intents are reviewable
- [ ] Intents do not trigger execution

---

### 3.2 XRP as a Planning Asset (Optional but Powerful)

**Feature:** Allow XRP balance to appear in:
- Net worth
- Planning views

**Checklist:**
- [ ] XRP marked as non-spendable from app
- [ ] Budgeting never assumes execution ability
- [ ] Clear disclaimer: "NextLedger cannot move XRP"

*This reinforces authority without action.*

---

## 4. Explicit Non-Features (Must Be Documented)

These must be stated in code comments + Help:

- ❌ Send XRP
- ❌ Sign XRPL transactions
- ❌ Trust lines
- ❌ DEX trades
- ❌ NFTs
- ❌ Hooks / smart contract interaction

**This is not a wallet.**

---

## 5. UX Copy Requirements (Critical)

Every XRPL surface must reinforce:

- [ ] "Read-only"
- [ ] "Non-custodial"
- [ ] "You approve everything"
- [ ] "Nothing is sent automatically"

### Bad phrasing:
- "Connect wallet"
- "Manage XRP"

### Good phrasing:
- "Track XRPL address"
- "View on-chain balance"

---

## 6. Diagnostics & Trust

**Checklist:**
- [ ] Diagnostics show:
  - [ ] XRPL endpoint
  - [ ] Last successful call
  - [ ] Error reason (sanitized)
- [ ] Copy diagnostics includes XRPL status
- [ ] No secrets ever logged

---

## 7. Phase Exit Criteria (XRPL v1 Complete)

You may declare XRPL support "done" when:

- [ ] Users can add XRPL addresses
- [ ] Balances display correctly
- [ ] Reserve is explained clearly
- [ ] XRPL transactions can be viewed or imported
- [ ] XRPL balances appear in net worth
- [ ] No execution code exists
- [ ] Checklist gates remain unviolated

At that point, NextLedger has real Web3 value.

---

## Why XRP Is the Right First Chain

- Account-based (easy mental model)
- Deterministic balances
- Clear reserve rules
- Mature RPC APIs
- Enterprise credibility
- Regulatory clarity relative to others

**XRPL fits a ledger-first worldview better than most chains.**

---

## What This Proves

If you ship just this:

- You've proven the Future Ledger concept
- You've differentiated from budgeting apps
- You've avoided wallet risk
- You've built a real Web3 bridge

And you've done it without crossing execution boundaries.

---

## Progress Tracking

| Feature | Status | Notes |
|---------|--------|-------|
| XRPL Client Hardening | ⬜ Not Started | |
| XRP Account Model | ⬜ Not Started | |
| Balance Fetch | ⬜ Not Started | |
| Balance Explanation | ⬜ Not Started | |
| Transaction History Import | ⬜ Not Started | |
| XRPL ↔ Ledger Reconciliation | ⬜ Not Started | |
| Intent Layer | ⬜ Not Started | |
| Diagnostics Integration | ⬜ Not Started | |

---

*Last updated: 2025*
*Depends on: [LEDGER_EXECUTION_CHECKLIST.md](LEDGER_EXECUTION_CHECKLIST.md)*
