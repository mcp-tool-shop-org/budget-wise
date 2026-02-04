# Verify a Release (NextLedger)

This is the runbook for validating an RC build on a clean machine or a clean user profile.

## Required evidence

- Store screenshots/recordings under `docs/phase12/screenshots/commit-##/`
- Update `docs/PHASE12_AUDIT.md` with:
  - what you verified
  - test output summary
  - links to evidence

## 1) Clean machine / profile setup

- Windows 10/11
- No debugger attached during verification

## 2) Install / launch

- [ ] Install or unzip the artifact
- [ ] Launch the app
- [ ] App reaches main shell without errors

Capture:
- [ ] First launch screenshot

## 3) Persistence check

NextLedger stores its local DB under:

- `%LOCALAPPDATA%\NextLedger\NextLedger.db`

Verify:

- [ ] Data persists across app restart
- [ ] App behaves sensibly if the DB is missing (fresh start)

Capture:
- [ ] Screenshot of diagnostics / DB path (if available)

## 4) Manual smoke test

Run the full manual script:

- `PHASE5_SANITY_TEST.md`

Record:

- [ ] Pass/fail
- [ ] Top 3 issues (if any)

## 5) Upgrade / uninstall (if applicable)

If you have a previous build installed:

- [ ] Upgrade to the new RC
- [ ] Confirm data remains intact

Then:

- [ ] Uninstall
- [ ] Note whether local data is removed or retained (document expected behavior)

Capture:

- [ ] Screenshot of the app version/build (if visible)

## 6) Diagnostics safety

- [ ] "Copy diagnostics" output contains no secrets
- [ ] If it includes paths/usernames, ensure guidance exists to redact before sharing

## Exit criteria

The release is accepted when:

- CI is green
- Manual smoke test passes (or remaining issues are explicitly accepted)
- Evidence is stored and linked from the Phase 12 audit log
