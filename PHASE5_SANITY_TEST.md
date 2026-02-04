# Phase 5 — Final Human Sanity Test (No Debugger)

Date: 2026-02-03  
Tester: ____________________  
Build: _____________________ (Debug/Release + git SHA if available)

**Rules**
- Run this *without* the debugger attached.
- Don’t “fix as you go” unless something truly blocks completion.
- Write down every “did that work?” moment.

---

## 0) Preflight

- [ ] App launches without errors
- [ ] Window title shows **BudgetWise**
- [ ] Global notifications (InfoBar) are visible when triggered

**Notes:**

---

## 1) Fresh Start / First Run

Goal: A reasonable human can start without guidance.

### Steps
- [ ] Ensure you’re starting from a clean state (new install or cleared app data / new DB)
- [ ] Launch the app

### Expected
- [ ] Starter data exists OR empty states clearly explain what to do next
- [ ] There is at least one on-budget account available (e.g., “Checking”) OR you’re told how to proceed
- [ ] Budget page is understandable (no blank/unclear screens)

**Notes:**

---

## 2) Add Income

Goal: Income entry feels obvious and results are reflected.

### Steps
- [ ] Go to Transactions
- [ ] Select an on-budget account
- [ ] Enter Payee (e.g., “Paycheck”)
- [ ] Enter Amount (e.g., 1000)
- [ ] Check **Inflow**
- [ ] Click **Add**

### Expected
- [ ] Success message appears
- [ ] Transaction shows in the list
- [ ] Budget / Ready-to-Assign reflects added income

**Notes:**

---

## 3) Allocate Budget

Goal: Allocation updates are clear and consistent.

### Steps
- [ ] Go to Budget
- [ ] Allocate amounts to 2–3 envelopes (e.g., Groceries, Utilities)

### Expected
- [ ] Ready-to-Assign decreases accordingly
- [ ] Envelope available amounts update immediately
- [ ] No confusing formatting (money, negatives)

**Notes:**

---

## 4) Add Simple Outflow

Goal: Outflow reduces envelope availability.

### Steps
- [ ] Go to Transactions
- [ ] Enter Payee (e.g., “Grocery Store”)
- [ ] Enter Amount (e.g., 50)
- [ ] Ensure **Inflow** is OFF
- [ ] Select an envelope (e.g., Groceries)
- [ ] Click **Add**

### Expected
- [ ] Success message appears
- [ ] Transaction appears with the envelope
- [ ] Envelope Available decreases by the outflow amount

**Notes:**

---

## 5) Create Split Transaction

Goal: Split workflow is understandable and guarded.

### Steps
- [ ] In Transactions, enter Payee (e.g., “Target”)
- [ ] Enter Amount (e.g., 120)
- [ ] Ensure **Inflow** is OFF
- [ ] Check **Split**
- [ ] Add split lines (e.g., Groceries 80, Dining 40)

### Expected
- [ ] Split summary updates live
- [ ] Split summary shows **OK** when totals match
- [ ] **Add** button is disabled until:
  - amounts are valid
  - envelopes chosen
  - split total equals the transaction amount
- [ ] After adding, the transaction shows as a split transaction

**Notes:**

---

## 6) Overspend an Envelope

Goal: Overspending is visible and not scary.

### Steps
- [ ] Create an outflow that exceeds an envelope’s available (or adjust allocations)

### Expected
- [ ] Overspent state is clearly indicated (e.g., red highlight)
- [ ] App continues to function normally
- [ ] Copy is calm and actionable

**Notes:**

---

## 7) CSV Import (Including a Duplicate)

Goal: Import is understandable, previews correctly, and prevents accidental duplicates.

### Steps
- [ ] Go to Import
- [ ] Select the same on-budget account
- [ ] Paste a CSV containing at least 3 rows, including:
  - one row that matches an existing transaction (duplicate)
  - two genuinely new rows
- [ ] Click **Preview**
- [ ] Click **Select new**
- [ ] Click **Import selected**
- [ ] Confirm in the dialog

### Expected
- [ ] Preview clearly marks rows as New vs Duplicate
- [ ] Import confirmation dialog is shown
- [ ] Only “New” rows are imported
- [ ] No raw exception text is shown on failure

**Notes:**

---

## 8) Reconcile an Account

Goal: Reconciliation is safe, explains differences, and confirmation exists.

### Steps
- [ ] Go to Reconcile
- [ ] Select an account
- [ ] Enter a statement ending balance
- [ ] Select some transactions as cleared
- [ ] Click **Reconcile**
- [ ] Confirm in the dialog

### Expected
- [ ] The “difference” calculation is clear
- [ ] Confirmation dialog appears before finalizing
- [ ] If adjustment is enabled, it is explained and not surprising

**Notes:**

---

## 9) Restart + Persistence Check

Goal: Nothing is “fake”; all key state persists.

### Steps
- [ ] Close the app
- [ ] Re-open the app

### Expected
- [ ] Accounts/envelopes still present
- [ ] Transactions list still present
- [ ] Budget allocations and available balances still make sense
- [ ] Overspending display remains consistent

**Notes:**

---

## 10) Error Handling / Diagnostics Spot Check

Goal: Unexpected failures are recoverable and don’t leak internals.

### Steps (pick one)
- [ ] Trigger a failure intentionally (e.g., bad XRPL URL, network off, etc.)
- [ ] Or reproduce any known edge-case

### Expected
- [ ] Friendly error message (no stack traces / raw exception messages)
- [ ] Global notification offers **Copy diagnostics** where appropriate
- [ ] Copied diagnostics do not include your full user path / sensitive data

**Notes:**

---

# Pass/Fail Summary

- Overall: [ ] PASS  [ ] FAIL

## Top Issues Found (ranked)
1. ________________________________
2. ________________________________
3. ________________________________

## “Did that work?” Moments
- ________________________________
- ________________________________

## Follow-up Fixes Requested
- ________________________________
- ________________________________
