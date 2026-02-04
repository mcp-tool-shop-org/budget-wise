using Microsoft.UI.Xaml;
using NextLedger.App.Services.Notifications;
using NextLedger.App.Services;
using NextLedger.App.Views;
using NextLedger.App.Views.Diagnostics;
using NextLedger.App.Views.About;
using NextLedger.App.Views.Help;
using NextLedger.App.Views.Import;
using NextLedger.App.Views.Reconciliation;
using NextLedger.App.Views.Spending;
using NextLedger.App.Views.Transactions;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using NextLedger.Infrastructure.Database;
using NextLedger.Infrastructure.Web3;
using System.Reflection;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics;
using WinRT.Interop;

namespace NextLedger.App;

public sealed partial class MainWindow : Window
{
    private readonly INotificationService _notifications;
    private readonly SqliteConnectionFactory _connectionFactory;
    private readonly Web3Options _web3Options;
    private readonly IEngineMetricsSink _sink;
    private readonly IAppSettingsService _settings;
    private CancellationTokenSource? _dismissCts;
    private Guid _notificationId;
    private AppWindow? _appWindow;
    private bool _isClampingSize;

    public MainWindow(
        INotificationService notifications,
        SqliteConnectionFactory connectionFactory,
        Web3Options web3Options,
        IEngineMetricsSink sink,
        IAppSettingsService settings)
    {
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _web3Options = web3Options ?? throw new ArgumentNullException(nameof(web3Options));
        _sink = sink ?? throw new ArgumentNullException(nameof(sink));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        InitializeComponent();

        ConfigureWindowChrome();

        _notifications.NotificationRaised += OnNotificationRaised;

        Nav.SelectedItem = Nav.MenuItems[0];
        RootFrame.Navigate(typeof(BudgetPage));

        // Show welcome dialog on first run.
        if (!_settings.HasCompletedWelcome)
        {
            _ = ShowWelcomeDialogAsync();
        }
    }

    private async Task ShowWelcomeDialogAsync()
    {
        // Small delay to let the UI stabilize before showing dialog.
        await Task.Delay(500);

        WelcomeDialog.XamlRoot = Content.XamlRoot;
        await WelcomeDialog.ShowAsync();

        _settings.HasCompletedWelcome = true;
    }

    private void ConfigureWindowChrome()
    {
        // Title is also set in XAML, but set it here too for consistency across hosts.
        Title = "NextLedger";

        try
        {
            var hwnd = WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            _appWindow = AppWindow.GetFromWindowId(windowId);
            _appWindow.Title = "NextLedger";

            // Sensible defaults; users can resize freely above min.
            _appWindow.Resize(new SizeInt32(1200, 800));
            _appWindow.Changed += OnAppWindowChanged;
        }
        catch
        {
            // If we can't access AppWindow (older host / restricted env), keep running.
        }
    }

    private void OnAppWindowChanged(AppWindow sender, AppWindowChangedEventArgs args)
    {
        if (_isClampingSize)
            return;

        // Enforce a minimum usable size without relying on platform-specific window styles.
        const int minWidth = 980;
        const int minHeight = 640;

        var size = sender.Size;
        if (size.Width >= minWidth && size.Height >= minHeight)
            return;

        try
        {
            _isClampingSize = true;
            sender.Resize(new SizeInt32(Math.Max(size.Width, minWidth), Math.Max(size.Height, minHeight)));
        }
        finally
        {
            _isClampingSize = false;
        }
    }

    private void OnNotificationRaised(object? sender, NotificationMessage message)
    {
        var dispatcher = DispatcherQueue;
        if (dispatcher is null)
            return;

        dispatcher.TryEnqueue(() => ShowInfoBar(message));
    }

    private void ShowInfoBar(NotificationMessage message)
    {
        _dismissCts?.Cancel();
        _dismissCts?.Dispose();
        _dismissCts = null;

        _notificationId = Guid.NewGuid();
        var localId = _notificationId;

        GlobalInfoBar.Title = message.Title;
        GlobalInfoBar.Message = message.Message;
        GlobalInfoBar.Severity = message.Severity switch
        {
            NotificationSeverity.Success => InfoBarSeverity.Success,
            NotificationSeverity.Warning => InfoBarSeverity.Warning,
            NotificationSeverity.Error => InfoBarSeverity.Error,
            _ => InfoBarSeverity.Informational
        };

        GlobalInfoBar.ActionButton = CreateActionButton(message);
        GlobalInfoBar.IsOpen = true;

        var duration = message.Duration;
        if (!duration.HasValue && message.Severity != NotificationSeverity.Error)
            duration = TimeSpan.FromSeconds(4);

        if (duration.HasValue)
        {
            _dismissCts = new CancellationTokenSource();
            var ct = _dismissCts.Token;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(duration.Value, ct);
                }
                catch (TaskCanceledException)
                {
                    return;
                }

                DispatcherQueue.TryEnqueue(() =>
                {
                    if (_notificationId == localId)
                        GlobalInfoBar.IsOpen = false;
                });
            }, ct);
        }
    }

    private ButtonBase? CreateActionButton(NotificationMessage message)
    {
        if (message.ActionKind == NotificationActionKind.None)
            return null;

        if (string.IsNullOrWhiteSpace(message.ActionLabel))
            return null;

        var button = new Button { Content = message.ActionLabel };
        button.Click += async (_, _) => await HandleNotificationActionAsync(message.ActionKind);
        return button;
    }

    private Task HandleNotificationActionAsync(NotificationActionKind actionKind)
    {
        switch (actionKind)
        {
            case NotificationActionKind.OpenDiagnostics:
                NavigateTo("diagnostics");
                GlobalInfoBar.IsOpen = false;
                return Task.CompletedTask;
            case NotificationActionKind.CopyDiagnostics:
                CopyDiagnostics();
                GlobalInfoBar.IsOpen = false;
                return Task.CompletedTask;
            default:
                return Task.CompletedTask;
        }
    }

    private void NavigateTo(string tag)
    {
        var item = Nav.MenuItems
            .OfType<NavigationViewItem>()
            .FirstOrDefault(i => string.Equals(i.Tag as string, tag, StringComparison.Ordinal));

        item ??= Nav.FooterMenuItems
            .OfType<NavigationViewItem>()
            .FirstOrDefault(i => string.Equals(i.Tag as string, tag, StringComparison.Ordinal));

        if (item is not null)
            Nav.SelectedItem = item;
        else
            RootFrame.Navigate(typeof(DiagnosticsPage));
    }

    private void CopyDiagnostics()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        var appVersionText = version is null ? "(unknown)" : version.ToString();

        var xrplRpcUrlText = string.IsNullOrWhiteSpace(_web3Options.RpcUrl) ? "(not set)" : SanitizeUrl(_web3Options.RpcUrl);
        var web3StatusText = string.IsNullOrWhiteSpace(_web3Options.RpcUrl) ? "Disabled" : "Enabled";

        var lines = new List<string>
        {
            $"AppVersion: {appVersionText}",
            $"DatabasePath: {RedactPath(_connectionFactory.DatabasePath)}",
            $"Web3Status: {web3StatusText}",
            $"XrplRpcUrl: {xrplRpcUrlText}",
            $"RecentEngineCalls: {_sink.GetRecent(100).Count}",
            $"UtcNow: {DateTime.UtcNow:O}"
        };

        var text = string.Join(Environment.NewLine, lines);
        var package = new DataPackage();
        package.SetText(text);
        Clipboard.SetContent(package);

        _notifications.ShowSuccess("Copied", "Diagnostics copied to clipboard.");
    }

    private static string RedactPath(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        var parts = value.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        for (var i = 0; i < parts.Length - 1; i++)
        {
            if (string.Equals(parts[i], "Users", StringComparison.OrdinalIgnoreCase))
            {
                parts[i + 1] = "<user>";
                break;
            }
        }

        return string.Join(Path.DirectorySeparatorChar, parts);
    }

    private static string SanitizeUrl(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "(not set)";

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
            return "(configured)";

        var builder = new UriBuilder(uri)
        {
            UserName = string.Empty,
            Password = string.Empty,
            Query = string.Empty,
            Fragment = string.Empty
        };

        return builder.Uri.ToString();
    }

    private void Nav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is not NavigationViewItem item)
            return;

        switch (item.Tag as string)
        {
            case "transactions":
                RootFrame.Navigate(typeof(TransactionsPage));
                break;
            case "import":
                RootFrame.Navigate(typeof(ImportPage));
                break;
            case "spending":
                RootFrame.Navigate(typeof(SpendingPage));
                break;
            case "reconcile":
                RootFrame.Navigate(typeof(ReconciliationPage));
                break;
            case "settings":
                RootFrame.Navigate(typeof(Views.Settings.SettingsPage));
                break;
            case "help":
                RootFrame.Navigate(typeof(HelpPage));
                break;
            case "about":
                RootFrame.Navigate(typeof(AboutPage));
                break;
            case "diagnostics":
                RootFrame.Navigate(typeof(DiagnosticsPage));
                break;
            case "budget":
            default:
                RootFrame.Navigate(typeof(BudgetPage));
                break;
        }
    }
}
