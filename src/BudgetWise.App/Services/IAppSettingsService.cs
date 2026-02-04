namespace BudgetWise.App.Services;

/// <summary>
/// Persists user preferences and app state across sessions.
/// </summary>
public interface IAppSettingsService
{
    /// <summary>
    /// Whether the user has completed the first-run welcome experience.
    /// </summary>
    bool HasCompletedWelcome { get; set; }

    /// <summary>
    /// Saves all settings to disk.
    /// </summary>
    void Save();
}
