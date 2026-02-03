# BudgetWise

A Windows-first personal finance app using envelope budgeting methodology.

## What is Envelope Budgeting?

Envelope budgeting is a simple, proven method where you allocate your income into virtual "envelopes" for different spending categories. You can only spend what's in each envelope, making overspending impossible.

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

**Phase 1**: Core domain and business logic (in progress)

See [DESIGN.md](DESIGN.md) for detailed architecture and roadmap.

## Development

### Prerequisites

- Windows 10 (1809+) or Windows 11
- Visual Studio 2022 17.8+ with:
  - .NET Desktop Development workload
  - Windows App SDK C# Templates
- .NET 9 SDK

### Building

```bash
dotnet restore
dotnet build
```

### Running Tests

```bash
dotnet test
```

## License

MIT License - see LICENSE file for details.

## Author

Built by [mcp-tool-shop](https://github.com/mcp-tool-shop-org)
