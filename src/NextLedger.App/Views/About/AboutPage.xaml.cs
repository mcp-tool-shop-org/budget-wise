using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Diagnostics;
using System.Reflection;
using Windows.System;

namespace NextLedger.App.Views.About;

public sealed partial class AboutPage : Page
{
    public AboutPage()
    {
        InitializeComponent();
        LoadVersionInfo();
        LoadDataPath();
    }

    private void LoadVersionInfo()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        if (version is not null)
        {
            VersionText.Text = $"Version {version.Major}.{version.Minor}.{version.Build}";
        }
    }

    private void LoadDataPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appDir = Path.Combine(appData, "NextLedger");
        DataPathText.Text = appDir;
    }

    private async void OpenDataFolder_Click(object sender, RoutedEventArgs e)
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appDir = Path.Combine(appData, "NextLedger");

        // Ensure the folder exists
        Directory.CreateDirectory(appDir);

        // Open in Explorer
        await Launcher.LaunchFolderPathAsync(appDir);
    }
}
