# Release Process (BudgetWise)

This doc defines how we cut Release Candidates (RCs) and stable releases for BudgetWise.

## Goals

- Reproducible builds
- Clear versioning
- Evidence-backed confidence (Phase 12 audit)
- Tester-friendly artifacts

## Versioning

We use SemVer for public versions.

- RC tags: `vX.Y.Z-rc.N` (example: `v0.1.0-rc.1`)
- Stable tags: `vX.Y.Z`

Rule: Every user-visible change updates `CHANGELOG.md`.

## Branching

- Default: `main`
- Optional for RC stabilization: `release/vX.Y.Z-rc.N`

## Pre-flight checklist

Before cutting an RC:

- `dotnet build BudgetWise.sln -c Release`
- `dotnet test BudgetWise.sln -c Release`
- Update `CHANGELOG.md`
- Update `docs/PHASE12_AUDIT.md` (Phase 12)
- Capture screenshots/recordings under `docs/phase12/screenshots/commit-##/`

## Artifact strategy (WinUI)

BudgetWise is a WinUI 3 app.

### Option A — Unpackaged (fastest for internal testing)

- Build: `dotnet build .\src\BudgetWise.App\BudgetWise.App.csproj -c Release`
- Ship: zip the output folder under `src/ BudgetWise.App/bin/Release/...` and include a short "how to run" note.

Pros: easy.
Cons: not a true installer experience.

### Option B — MSIX (preferred for external testers)

MSIX packaging typically requires Visual Studio tooling and/or a packaging project.

If MSIX packaging is not present in the repo yet, document the current packaging method and treat MSIX as a Phase 12/13 deliverable.

## Cutting an RC (GitHub)

1) Ensure `main` is green in CI.
2) Update `CHANGELOG.md` for the RC.
3) Create tag:

- `git tag vX.Y.Z-rc.N`
- `git push origin vX.Y.Z-rc.N`

4) Create GitHub Release:

- Title: `vX.Y.Z-rc.N`
- Notes: copy from `CHANGELOG.md`
- Attach artifacts (zip/MSIX) + checksums

5) Record verification evidence:

- Follow `docs/VERIFY_RELEASE.md`
- Store screenshots/recordings under the Phase 12 evidence folder

## Post-RC rules

- RC fixes must include:
  - changelog line
  - test evidence
  - updated Phase 12 audit entry (if in Phase 12)
