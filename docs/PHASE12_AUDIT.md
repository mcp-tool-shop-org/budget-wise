# Phase 12 Audit — Release Candidate Discipline (NextLedger)

Phase 12 is about proving **NextLedger** is stable, releasable, supportable, and resilient in real environments — with evidence you can point to later.

Companion docs:

- `docs/PHASE12_CHECKLIST.md`
- `docs/RELEASE_PROCESS.md`
- `docs/VERIFY_RELEASE.md`
- `docs/RC_DISCIPLINE_CHECKLIST.md`

## Global Evidence Rule (applies to every commit in Phase 12)

For every Phase 12 commit:

1) Store before/after screenshots in:

- `docs/phase12/screenshots/commit-##/`

2) Update this file (`docs/PHASE12_AUDIT.md`) with:

- what changed
- test evidence
- screenshot links
- any remaining known issues

3) For “soak” commits, add:

- a 30–90 second screen recording (GIF/MP4) showing the flow (store under the same commit folder)

## Evidence types we accept

- `dotnet test -c Release` output (paste summary + counts)
- `dotnet build NextLedger.sln -c Release` output summary
- Manual run evidence from `PHASE5_SANITY_TEST.md`
- Screenshots/GIFs of key UI states
- If a build prerequisite exists (WinUI toolchain), capture it in docs + screenshots

## Commit audit log

> Tip: Replace `TBD` with the actual commit SHA once created.

### Commit 01 — Baseline repo hygiene & governance

- **Commit:** TBD
- **Goal:** Make the repo feel legitimate and support-ready.
- **What changed:**
  - TBD
- **Test evidence:**
  - TBD
- **Screenshots:**
  - `docs/phase12/screenshots/commit-01/` (add links to files)
- **Known issues / follow-ups:**
  - TBD

### Commit 02 — RC versioning + release notes discipline

- **Commit:** TBD
- **Goal:** Make versioning and release notes non-negotiable.
- **What changed:**
  - TBD
- **Test evidence:**
  - TBD
- **Screenshots:**
  - `docs/phase12/screenshots/commit-02/`
- **Known issues / follow-ups:**
  - TBD

### Commit 03 — Cold machine install/upgrade/uninstall runbook

- **Commit:** TBD
- **Goal:** Kill “works on my machine” for NextLedger WinUI desktop.
- **What changed:**
  - TBD
- **Test evidence:**
  - TBD
- **Screenshots:**
  - `docs/phase12/screenshots/commit-03/`
- **Known issues / follow-ups:**
  - TBD

### Commit 04 — Soak test harness (stability over time)

- **Commit:** TBD
- **Goal:** Prove NextLedger stays responsive and doesn’t leak.
- **What changed:**
  - TBD
- **Test evidence:**
  - TBD
- **Screenshots/Recording:**
  - `docs/phase12/screenshots/commit-04/` (include start vs end memory view)
- **Known issues / follow-ups:**
  - TBD

### Commit 05 — Crash reporting + last session recovery UX

- **Commit:** TBD
- **Goal:** When something goes wrong, users feel protected.
- **What changed:**
  - TBD
- **Test evidence:**
  - TBD
- **Screenshots:**
  - `docs/phase12/screenshots/commit-05/`
- **Known issues / follow-ups:**
  - TBD

### Commit 06 — End-to-end button coverage tests

- **Commit:** TBD
- **Goal:** Prevent “dead UI” regressions.
- **What changed:**
  - TBD
- **Test evidence:**
  - TBD
- **Screenshots:**
  - `docs/phase12/screenshots/commit-06/`
- **Known issues / follow-ups:**
  - TBD

### Commit 07 — Help Center → Troubleshooting Assistant

- **Commit:** TBD
- **Goal:** Make in-app help actionable for NextLedger.
- **NextLedger-specific scope:**
  - Budget math / Ready-to-Assign confusion
  - Import duplicates / date parsing
  - Reconciliation difference not zero
  - WinUI build prerequisites (for devs)
  - Optional Web3/XRPL diagnostics status
- **What changed:**
  - TBD
- **Test evidence:**
  - TBD
- **Screenshots:**
  - `docs/phase12/screenshots/commit-07/`
- **Known issues / follow-ups:**
  - TBD

### Commit 08 — UX consistency audit (light/dark + contrast + density)

- **Commit:** TBD
- **Goal:** Ensure NextLedger feels intentional in both themes.
- **What changed:**
  - TBD
- **Test evidence:**
  - TBD
- **Screenshots (mandatory set):**
  - Main shell: dark vs light
  - Budget page: dark vs light
  - Transactions page: dark vs light
  - Import page: dark vs light
  - Reconciliation page: dark vs light
  - Diagnostics page: dark vs light
  - Focus ring visibility
  - Store under `docs/phase12/screenshots/commit-08/`
- **Known issues / follow-ups:**
  - TBD

### Commit 09 — Release artifact proof pack

- **Commit:** TBD
- **Goal:** Every RC build produces a proof bundle.
- **NextLedger-specific artifacts:**
  - MSIX (or signed unpackaged zip if MSIX isn’t ready)
  - checksums
  - audit snapshot
  - sanitized support bundle example
- **What changed:**
  - TBD
- **Test evidence:**
  - TBD
- **Screenshots:**
  - `docs/phase12/screenshots/commit-09/`
- **Known issues / follow-ups:**
  - TBD

### Commit 10 — RC1 cut + beta readiness gate

- **Commit:** TBD
- **Goal:** Tag and publish RC1 with a clear beta guide.
- **What changed:**
  - TBD
- **Test evidence:**
  - TBD
- **Screenshots:**
  - `docs/phase12/screenshots/commit-10/`
- **Known issues / follow-ups:**
  - TBD

## Phase 12 completion definition

Phase 12 is complete when:

- CI builds + tests on a clean runner
- Cold machine install/upgrade/uninstall is validated (runbook + evidence)
- Soak tests show stability over time
- Help content can diagnose common failures
- UI smoke tests cover all top-level UI actions
- Light + dark mode are both production quality
- RC1 is tagged and published with a beta guide
