using NextLedger.App.Services;
using NextLedger.App.Services.Notifications;
using System.Text.Json;
using NextLedger.Application.Interfaces;
using NextLedger.Infrastructure.Database;
using NextLedger.Infrastructure.Web3;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Reflection;
using Windows.ApplicationModel.DataTransfer;

namespace NextLedger.App.ViewModels.Diagnostics;

public sealed partial class DiagnosticsViewModel : ObservableObject
{
    private readonly IEngineMetricsSink _sink;
    private readonly IXrplClient _xrpl;
    private readonly Web3Options _web3Options;
    private readonly SqliteConnectionFactory _connectionFactory;
    private readonly INotificationService _notifications;

    public DiagnosticsViewModel(
        IEngineMetricsSink sink,
        IXrplClient xrpl,
        Web3Options web3Options,
        SqliteConnectionFactory connectionFactory,
        INotificationService notifications)
    {
        _sink = sink ?? throw new ArgumentNullException(nameof(sink));
        _xrpl = xrpl ?? throw new ArgumentNullException(nameof(xrpl));
        _web3Options = web3Options ?? throw new ArgumentNullException(nameof(web3Options));
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
    }

    [ObservableProperty]
    private IReadOnlyList<EngineCallMetricRow> _recent = Array.Empty<EngineCallMetricRow>();

    [ObservableProperty]
    private bool _isXrplLoading;

    [ObservableProperty]
    private string _xrplAccountAddress = string.Empty;

    [ObservableProperty]
    private string _xrplResultJson = string.Empty;

    [ObservableProperty]
    private string _xrplErrorText = string.Empty;

    public string XrplRpcUrlText
        => string.IsNullOrWhiteSpace(_web3Options.RpcUrl) ? "(not set)" : SanitizeUrl(_web3Options.RpcUrl);

    public string Web3StatusText
        => string.IsNullOrWhiteSpace(_web3Options.RpcUrl) ? "Disabled" : "Enabled";

    public string DatabasePathText
        => RedactPath(_connectionFactory.DatabasePath);

    public string AppVersionText
    {
        get
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            return version is null ? "(unknown)" : version.ToString();
        }
    }

    [RelayCommand]
    private void Refresh()
    {
        var rows = _sink.GetRecent(100)
            .OrderByDescending(m => m.Timestamp)
            .Select(m => new EngineCallMetricRow
            {
                WhenUtc = m.Timestamp.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                Operation = m.Operation,
                ElapsedMs = m.Elapsed.TotalMilliseconds.ToString("0.0"),
                Success = m.Success,
                ErrorCode = m.ErrorCode
            })
            .ToList();

        Recent = rows;
    }

    [RelayCommand]
    private async Task XrplServerInfoAsync()
    {
        await RunXrplCallAsync(() => _xrpl.GetServerInfoAsync());
    }

    [RelayCommand]
    private async Task XrplAccountInfoAsync()
    {
        if (string.IsNullOrWhiteSpace(XrplAccountAddress))
        {
            XrplErrorText = "Enter an XRPL address (starts with r...).";
            XrplResultJson = string.Empty;
            _notifications.ShowWarning("Missing address", "Enter an XRPL address and try again.");
            return;
        }

        await RunXrplCallAsync(() => _xrpl.GetAccountInfoAsync(XrplAccountAddress));
    }

    private async Task RunXrplCallAsync(Func<Task<Web3RpcResponse<JsonElement>>> call)
    {
        IsXrplLoading = true;
        XrplErrorText = string.Empty;
        XrplResultJson = string.Empty;

        try
        {
            var response = await call();
            if (!response.Success)
            {
                var code = response.Error?.Code;
                XrplErrorText = code is null
                    ? "XRPL endpoint returned an error."
                    : $"XRPL endpoint returned an error ({code}).";

                _notifications.ShowErrorAction(
                    "XRPL request failed",
                    "Try again. If it keeps happening, copy diagnostics and share them with support.",
                    NotificationActionKind.CopyDiagnostics,
                    "Copy diagnostics");
                return;
            }

            if (response.Result.ValueKind == JsonValueKind.Undefined)
            {
                XrplErrorText = "XRPL call succeeded, but returned no result.";
                _notifications.ShowWarning("No result", "The XRPL endpoint returned no result. See details below.");
                return;
            }

            XrplResultJson = JsonSerializer.Serialize(response.Result, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
        catch (Exception)
        {
            XrplErrorText = "XRPL request failed due to a network or endpoint error.";
            _notifications.ShowErrorAction(
                "XRPL request failed",
                "Try again. If it keeps happening, copy diagnostics and share them with support.",
                NotificationActionKind.CopyDiagnostics,
                "Copy diagnostics");
        }
        finally
        {
            IsXrplLoading = false;
        }
    }

    [RelayCommand]
    private void CopyDiagnostics()
    {
        var lines = new List<string>
        {
            $"AppVersion: {AppVersionText}",
            $"DatabasePath: {DatabasePathText}",
            $"Web3Status: {Web3StatusText}",
            $"XrplRpcUrl: {XrplRpcUrlText}",
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

        // Best-effort redaction for user profile paths.
        // Example: C:\Users\Alice\AppData\... -> C:\Users\<user>\AppData\...
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

        // Drop userinfo and query/fragment to avoid accidentally leaking secrets.
        var builder = new UriBuilder(uri)
        {
            UserName = string.Empty,
            Password = string.Empty,
            Query = string.Empty,
            Fragment = string.Empty
        };

        return builder.Uri.ToString();
    }
}

public sealed record EngineCallMetricRow
{
    public required string WhenUtc { get; init; }
    public required string Operation { get; init; }
    public required string ElapsedMs { get; init; }
    public required bool Success { get; init; }
    public string? ErrorCode { get; init; }
}
