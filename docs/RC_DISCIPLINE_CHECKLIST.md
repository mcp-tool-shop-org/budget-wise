# RC Discipline Checklist (BudgetWise)

This is the "do not ship without this" checklist.

## Always

- [ ] CI passes on a clean runner (build + test)
- [ ] Manual smoke test complete: `PHASE5_SANITY_TEST.md`
- [ ] No raw exception text shown to end users
- [ ] Diagnostics are safe to share (no secrets)
- [ ] `CHANGELOG.md` updated

## Phase 12-specific

- [ ] `docs/PHASE12_AUDIT.md` updated with evidence
- [ ] Screenshots/recordings stored under `docs/phase12/screenshots/commit-##/`
- [ ] Release verification runbook completed: `docs/VERIFY_RELEASE.md`

## Release artifacts

- [ ] Artifact produced (zip or MSIX)
- [ ] Checksums produced
- [ ] GitHub Release created with notes + attachments

## Tagging

- [ ] RC tag: `vX.Y.Z-rc.N`
- [ ] Stable tag: `vX.Y.Z`
