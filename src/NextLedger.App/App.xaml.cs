using NextLedger.App.Services;
using Microsoft.UI.Xaml;

namespace NextLedger.App;

public sealed partial class App : Microsoft.UI.Xaml.Application
{
    private AppHost? _host;
    private Window? _window;

    public App()
    {
        InitializeComponent();
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        _host = AppHost.Build();
        await _host.InitializeAsync();

        _window = _host.Services.GetRequiredService<MainWindow>();
        _window.Activate();
    }
}
