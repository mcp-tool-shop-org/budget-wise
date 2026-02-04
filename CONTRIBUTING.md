# Contributing to NextLedger

Thanks for helping improve NextLedger.

## Quick start

- Use Windows + Visual Studio 2022 for the WinUI project.
- Run tests: `dotnet test NextLedger.sln -c Release`

## Pull requests

- Keep PRs small and focused.
- Include tests for behavioral changes when practical.
- Update `CHANGELOG.md` for user-visible changes.
- If working in Phase 12, follow the evidence rule in `docs/PHASE12_AUDIT.md`.

## Code style

- Prefer clarity over cleverness.
- Avoid mixing business rules into UI; keep engine logic in the core layers.
