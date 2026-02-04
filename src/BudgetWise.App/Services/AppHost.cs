using BudgetWise.Application.Interfaces;
using BudgetWise.Application.Services;
using BudgetWise.App.ViewModels;
using BudgetWise.App.ViewModels.Diagnostics;
using BudgetWise.App.ViewModels.Import;
using BudgetWise.App.ViewModels.Reconciliation;
using BudgetWise.App.ViewModels.Spending;
using BudgetWise.App.ViewModels.Transactions;
using BudgetWise.App.Services.Notifications;
using BudgetWise.Infrastructure.Database;
using BudgetWise.Infrastructure.Repositories;
using BudgetWise.Infrastructure.Web3;
using BudgetWise.Domain.Entities;
using BudgetWise.Domain.Enums;
using BudgetWise.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BudgetWise.App.Services;

public sealed class AppHost
{
    private readonly IHost _host;

    private AppHost(IHost host)
    {
        _host = host;
    }

    public IServiceProvider Services => _host.Services;

    public static AppHost Current
        => _current ?? throw new InvalidOperationException("AppHost not initialized.");

    private static AppHost? _current;

    public static AppHost Build()
    {
        if (_current is not null)
            return _current;

        var builder = Host.CreateApplicationBuilder();

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appDir = Path.Combine(appData, "BudgetWise");
        Directory.CreateDirectory(appDir);
        var dbPath = Path.Combine(appDir, "budgetwise.db");

        builder.Services.AddSingleton(new SqliteConnectionFactory(dbPath));
        builder.Services.AddTransient<IUnitOfWork, UnitOfWork>();

        // Web3 infrastructure (optional; configured via env var).
        builder.Services.AddSingleton(new Web3Options
        {
            RpcUrl = Environment.GetEnvironmentVariable("BUDGETWISE_WEB3_RPC_URL") ?? string.Empty
        });
        builder.Services.AddSingleton<HttpClient>();
        builder.Services.AddSingleton<IWeb3Client>(sp =>
        {
            var options = sp.GetRequiredService<Web3Options>();
            if (string.IsNullOrWhiteSpace(options.RpcUrl))
                return new NullWeb3Client();

            return new EthereumJsonRpcClient(sp.GetRequiredService<HttpClient>(), options);
        });
        builder.Services.AddSingleton<IXrplClient, XrplClient>();

        // Engine orchestration surface (UI must go through this).
        // Wrap in instrumentation so we can measure call latency before building richer UI.
        builder.Services.AddSingleton<IEngineMetricsSink>(_ => new InMemoryEngineMetricsSink());
        builder.Services.AddTransient<BudgetEngine>();
        builder.Services.AddTransient<IBudgetEngine>(sp =>
            new InstrumentedBudgetEngine(
                sp.GetRequiredService<BudgetEngine>(),
                sp.GetRequiredService<IEngineMetricsSink>(),
                sp.GetRequiredService<ILogger<InstrumentedBudgetEngine>>()));

        // Global notifications + user-friendly error mapping.
        builder.Services.AddSingleton<INotificationService, NotificationService>();
        builder.Services.AddSingleton<IEngineErrorMessageMapper, EngineErrorMessageMapper>();

        // UI
        builder.Services.AddSingleton<MainWindow>();
        builder.Services.AddTransient<BudgetViewModel>();
        builder.Services.AddTransient<DiagnosticsViewModel>();
        builder.Services.AddTransient<TransactionsViewModel>();
        builder.Services.AddTransient<SpendingViewModel>();
        builder.Services.AddTransient<ReconciliationViewModel>();
        builder.Services.AddTransient<ImportViewModel>();

        _current = new AppHost(builder.Build());
        return _current;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        var connectionFactory = Services.GetRequiredService<SqliteConnectionFactory>();
        await connectionFactory.InitializeDatabaseAsync(ct);

        await SeedDefaultsIfEmptyAsync(ct);
    }

    private async Task SeedDefaultsIfEmptyAsync(CancellationToken ct)
    {
        // UX: first launch should feel intentional and usable.
        using var uow = Services.GetRequiredService<IUnitOfWork>();

        var accounts = await uow.Accounts.GetActiveAccountsAsync(ct);
        if (accounts.Count == 0)
        {
            var checking = Account.Create("Checking", AccountType.Checking, Money.USD(0m), isOnBudget: true);
            await uow.Accounts.AddAsync(checking, ct);
        }

        var envelopes = await uow.Envelopes.GetActiveEnvelopesAsync(ct);
        if (envelopes.Count == 0)
        {
            var starter = new[]
            {
                Envelope.Create("Rent / Mortgage", "Bills"),
                Envelope.Create("Utilities", "Bills"),
                Envelope.Create("Groceries", "Everyday"),
                Envelope.Create("Dining", "Everyday"),
                Envelope.Create("Transportation", "Everyday"),
                Envelope.Create("Emergency Fund", "Savings"),
            };

            foreach (var env in starter)
                await uow.Envelopes.AddAsync(env, ct);
        }
    }
}
