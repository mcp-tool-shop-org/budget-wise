# NextLedger

<p align="center">
  <img src="src/NextLedger.App/Assets/NextLedger_256.png" alt="NextLedger Icon" width="128" height="128">
</p>

**Envelope budgeting for Windows — give every dollar a job.**

A Windows-first personal finance app using envelope budgeting methodology. Your data stays local, no cloud required. Built as a **future ledger** — an authoritative system of financial truth with explicit human agency at every boundary.

## Download

📦 **[Latest Release](https://github.com/mcp-tool-shop-org/NextLedger/releases/latest)**

Download the ZIP, extract, and run `NextLedger.App.exe`. No installation required.

## What is Envelope Budgeting?

Envelope budgeting is a simple, proven method where you allocate your income into virtual "envelopes" for different spending categories. You can only spend what's in each envelope, making overspending impossible.

## Features

- **Offline-First**: Your data stays on your machine. No cloud required.
- **Envelope Budgeting**: Allocate every dollar to a purpose
- **Multiple Accounts**: Track checking, savings, credit cards, cash
- **Transaction Tracking**: Categorize and search your spending
- **CSV Import**: Import bank statements easily
- **Reconciliation**: Match your records with bank statements
- **Windows Native**: Built with WinUI 3 for a modern Windows experience

## Screenshots

*Coming soon*

## Documentation

- [Changelog](CHANGELOG.md)
- [Engine Error Codes](ENGINE_ERROR_CODES.md)
- [Release Process](docs/RELEASE_PROCESS.md)
- [Future Ledger Vision](docs/FUTURE_LEDGER_VISION.md)
- [Ledger Execution Checklist](docs/LEDGER_EXECUTION_CHECKLIST.md)

## Technology

- **UI**: WinUI 3 / Windows App SDK
- **Language**: C# / .NET 9
- **Database**: SQLite (local)
- **Architecture**: Clean Architecture with MVVM

## Project Status

✅ **v1.0.0** - Ready for release

Core functionality complete:
- Budget management with monthly allocations
- Transaction tracking with split support
- CSV import from bank statements
- Account reconciliation
- Spending analysis by envelope
- In-app help and guidance

See [DESIGN.md](DESIGN.md) for detailed architecture.

## Roadmap

NextLedger is evolving toward a **future ledger** — see [Future Ledger Vision](docs/FUTURE_LEDGER_VISION.md) for the full architecture.

| Layer | Status | Description |
|-------|--------|-------------|
| Observation | ✅ Complete | Local balances, transactions, accounts |
| Interpretation | ✅ Complete | Envelope budgeting, spending analysis |
| Intent Declaration | 🔜 Planned | Budget goals, allocation rules |
| Constraint Enforcement | 🔜 Planned | Budget limits, overspend protection |
| User-Approved Execution | 🔮 Future | Web3 integration (non-custodial) |

## Development

### Prerequisites

- Windows 10 (1809+) or Windows 11
- Visual Studio (2022 17.8+ or newer) with:
  - .NET Desktop Development workload
  - Windows App SDK C# Templates
  - Windows SDK / MSIX (Appx/PRI build tools)
- .NET 9 SDK

**Note on CLI builds (WinUI):** The WinUI project (`NextLedger.App`) runs Windows App SDK build steps that require the Appx/MSIX + PRI MSBuild task assemblies. If you see an error like `MSB4062` referencing missing `Microsoft.Build.AppxPackage.dll` or `Microsoft.Build.Packaging.Pri.Tasks.dll`, install the Windows SDK / MSIX components via the Visual Studio Installer (or build the app from within Visual Studio).

### Building

```bash
dotnet restore
dotnet build
```

### How to Run the App

**Visual Studio (recommended)**

1. Open `NextLedger.sln` in Visual Studio 2022.
2. Set `NextLedger.App` as the startup project.
3. Run with **F5**.

**CLI (build + launch)**

```bash
dotnet build .\src\NextLedger.App\NextLedger.App.csproj -c Debug
```

If this fails with `MSB4062`, see the note in **Prerequisites**.

Then run the generated exe from the build output folder under:

- `.\src\NextLedger.App\bin\Debug\net9.0-windows10.0.19041.0\`

**Local data location**

The app creates a local SQLite database at:

- `%LOCALAPPDATA%\NextLedger\NextLedger.db`

### Running Tests

```bash
dotnet test
```

## License

MIT License - see LICENSE file for details.

## Author

Built by [mcp-tool-shop](https://github.com/mcp-tool-shop-org)
