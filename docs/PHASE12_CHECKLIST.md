# Phase 12 Checklist — Release Candidate Discipline (BudgetWise)

This checklist is the execution companion to `docs/PHASE12_AUDIT.md`.

Rule: Every Phase 12 commit updates the audit log and stores evidence under `docs/phase12/screenshots/commit-##/`.

---

## 1) Repo hygiene & legitimacy

- [ ] `.github/workflows/ci.yml` exists and runs on PRs
- [ ] `.github` issue templates + PR template exist
- [ ] `LICENSE` present
- [ ] `SECURITY.md` present (reporting policy)
- [ ] `CODE_OF_CONDUCT.md` present
- [ ] `CONTRIBUTING.md` present
- [ ] `CHANGELOG.md` present and maintained

## 2) RC versioning & release notes discipline

- [ ] Versioning scheme documented (SemVer + RC tags)
- [ ] Release notes are sourced from `CHANGELOG.md`
- [ ] RC tags are consistent: `vX.Y.Z-rc.N`

## 3) Cold machine install/upgrade/uninstall runbook

- [ ] Document install steps for testers
- [ ] Document upgrade steps (keep user data)
- [ ] Document uninstall steps (what happens to local data)
- [ ] Evidence captured (screenshots)

## 4) Soak test discipline

- [ ] Defined a soak scenario (e.g., 30–60 min typical usage)
- [ ] Captured baseline vs end-state evidence (memory, responsiveness)
- [ ] Recorded a short clip for the soak flow

## 5) Crash recovery UX

- [ ] App handles crash on next launch with a calm recovery path
- [ ] No raw stack traces shown to end users
- [ ] Diagnostics can be copied safely

## 6) UI smoke coverage ("every button works")

- [ ] Top-level pages have explicit smoke coverage
- [ ] No dead buttons / silent failures
- [ ] Manual sanity test is current: `PHASE5_SANITY_TEST.md`

## 7) Help center / troubleshooting assistant

- [ ] Documents top user confusions (Ready-to-Assign, overspending, rollover)
- [ ] Import troubleshooting (duplicates, date parsing)
- [ ] Reconcile troubleshooting (difference not zero)
- [ ] Build prerequisites troubleshooting (WinUI CLI)

## 8) Theme + accessibility audit

- [ ] Light and dark mode reviewed on all key pages
- [ ] Focus rings visible and consistent
- [ ] Contrast meets expectations for important states (errors/overspending)

## 9) Release artifact proof pack

- [ ] Releasable artifact produced (MSIX or zipped unpackaged build)
- [ ] Checksums produced
- [ ] Proof pack includes: audit snapshot + screenshots

## 10) RC1 cut + beta readiness gate

- [ ] RC1 tag created
- [ ] GitHub release created with notes + artifacts
- [ ] Verification runbook completed: `docs/VERIFY_RELEASE.md`

---

## Phase 12 “Done” definition

Phase 12 is done when the checklist is complete AND the audit log contains evidence for each Phase 12 commit.
