using System.Text.Json;

namespace BudgetWise.App.Services;

/// <summary>
/// File-based settings persistence for the app.
/// </summary>
public sealed class AppSettingsService : IAppSettingsService
{
    private readonly string _settingsPath;
    private AppSettings _settings;

    public AppSettingsService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appDir = Path.Combine(appData, "BudgetWise");
        Directory.CreateDirectory(appDir);
        _settingsPath = Path.Combine(appDir, "settings.json");

        _settings = Load();
    }

    public bool HasCompletedWelcome
    {
        get => _settings.HasCompletedWelcome;
        set
        {
            _settings.HasCompletedWelcome = value;
            Save();
        }
    }

    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsPath, json);
        }
        catch
        {
            // Fail silently; user experience won't break, just won't persist.
        }
    }

    private AppSettings Load()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch
        {
            // Fail silently; return defaults.
        }

        return new AppSettings();
    }

    private sealed class AppSettings
    {
        public bool HasCompletedWelcome { get; set; }
    }
}
