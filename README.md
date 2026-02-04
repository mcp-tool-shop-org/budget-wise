# BudgetWise

A Windows-first personal finance app using envelope budgeting methodology.

## What is Envelope Budgeting?

Envelope budgeting is a simple, proven method where you allocate your income into virtual "envelopes" for different spending categories. You can only spend what's in each envelope, making overspending impossible.

## Planning

- Phase 3 checklist: [PHASE3_CHECKLIST.md](PHASE3_CHECKLIST.md)
- Phase 4 checklist: [PHASE4_CHECKLIST.md](PHASE4_CHECKLIST.md)
- Phase 5 checklist: [PHASE5_CHECKLIST.md](PHASE5_CHECKLIST.md)
- Phase 12 audit (RC discipline): [docs/PHASE12_AUDIT.md](docs/PHASE12_AUDIT.md)
- Phase 12 checklist: [docs/PHASE12_CHECKLIST.md](docs/PHASE12_CHECKLIST.md)
- RC discipline checklist: [docs/RC_DISCIPLINE_CHECKLIST.md](docs/RC_DISCIPLINE_CHECKLIST.md)
- Release process: [docs/RELEASE_PROCESS.md](docs/RELEASE_PROCESS.md)
- Verify a release: [docs/VERIFY_RELEASE.md](docs/VERIFY_RELEASE.md)
- Changelog: [CHANGELOG.md](CHANGELOG.md)
- Engine error codes catalog: [ENGINE_ERROR_CODES.md](ENGINE_ERROR_CODES.md)

## Features (Planned)

- **Offline-First**: Your data stays on your machine. No cloud required.
- **Envelope Budgeting**: Allocate every dollar to a purpose
- **Multiple Accounts**: Track checking, savings, credit cards, cash
- **Transaction Tracking**: Categorize and search your spending
- **Visual Reports**: See where your money goes
- **Windows Native**: Built with WinUI 3 for a modern Windows experience

## Technology

- **UI**: WinUI 3 / Windows App SDK
- **Language**: C# / .NET 9
- **Database**: SQLite (local)
- **Architecture**: Clean Architecture with MVVM

## Project Status

**Phase 2**: Budget engine + deterministic core (complete)

**Phase 3**: UI foundation & engine integration (planned)

See [DESIGN.md](DESIGN.md) for detailed architecture and roadmap.

## Development

### Prerequisites

- Windows 10 (1809+) or Windows 11
- Visual Studio (2022 17.8+ or newer) with:
  - .NET Desktop Development workload
  - Windows App SDK C# Templates
  - Windows SDK / MSIX (Appx/PRI build tools)
- .NET 9 SDK

**Note on CLI builds (WinUI):** The WinUI project (`BudgetWise.App`) runs Windows App SDK build steps that require the Appx/MSIX + PRI MSBuild task assemblies. If you see an error like `MSB4062` referencing missing `Microsoft.Build.AppxPackage.dll` or `Microsoft.Build.Packaging.Pri.Tasks.dll`, install the Windows SDK / MSIX components via the Visual Studio Installer (or build the app from within Visual Studio).

### Building

```bash
dotnet restore
dotnet build
```

### How to Run the App

**Visual Studio (recommended)**

1. Open `BudgetWise.sln` in Visual Studio 2022.
2. Set `BudgetWise.App` as the startup project.
3. Run with **F5**.

**CLI (build + launch)**

```bash
dotnet build .\src\BudgetWise.App\BudgetWise.App.csproj -c Debug
```

If this fails with `MSB4062`, see the note in **Prerequisites**.

Then run the generated exe from the build output folder under:

- `.\src\BudgetWise.App\bin\Debug\net9.0-windows10.0.19041.0\`

**Local data location**

The app creates a local SQLite database at:

- `%LOCALAPPDATA%\BudgetWise\budgetwise.db`

### Running Tests

```bash
dotnet test
```

## License

MIT License - see LICENSE file for details.

## Author

Built by [mcp-tool-shop](https://github.com/mcp-tool-shop-org)
