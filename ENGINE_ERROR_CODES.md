# Engine Error Codes Catalog

This document lists the stable error codes emitted by the `IBudgetEngine` orchestration layer (via `BudgetOperationResult` / `BudgetOperationError`).

## Contract Notes

- **Codes are stable**: UI should map behavior off `BudgetOperationError.Code`.
- **Messages are best-effort**: UI may display `Message`, but should not parse it.
- **Targets are optional**: When present, `Target` identifies the input field/parameter most closely associated with the error.

## Current Codes (Emitted)

### `VALIDATION`

**Meaning**
- The request payload is invalid (missing/invalid arguments).

**Typical UI handling**
- Show an inline validation message.
- If `Target` is present, highlight that field.

**Common causes**
- Null request objects.
- Invalid argument values.

**Target usage**
- Usually a parameter name (e.g., `envelopeId`, `amount`, `request`).

---

### `INVALID_OPERATION`

**Meaning**
- The requested action violates a business rule or current state (e.g., trying to mutate a closed period, attempting to allocate beyond Ready-to-Assign).

**Typical UI handling**
- Show a user-friendly error banner/toast.
- Keep current UI state unchanged (no optimistic updates).

**Common causes**
- Budget period is closed.
- Allocation/move would exceed available funds.

---

### `NOT_IMPLEMENTED`

**Meaning**
- The operation is wired but not yet implemented.

**Typical UI handling**
- Show a “not available yet” message.
- Optionally disable the triggering control until implemented.

---

### `UNEXPECTED`

**Meaning**
- An unhandled exception occurred.

**Typical UI handling**
- Show a generic failure message.
- Provide a retry action.
- Optionally offer a “copy diagnostics” button (message + correlation id, if later added).

## Where These Codes Come From

The engine maps exceptions to codes in the orchestration layer:
- `ArgumentNullException`, `ArgumentException` → `VALIDATION` (and sets `Target` when available)
- `InvalidOperationException` → `INVALID_OPERATION`
- `NotImplementedException` → `NOT_IMPLEMENTED`
- Anything else → `UNEXPECTED`

## UI Mapping Recommendation (Minimal)

- `VALIDATION`: inline field error when possible; otherwise show banner.
- `INVALID_OPERATION`: banner/toast with recovery hint (e.g., “Unassign some money first”).
- `NOT_IMPLEMENTED`: informational toast.
- `UNEXPECTED`: generic error + retry.
