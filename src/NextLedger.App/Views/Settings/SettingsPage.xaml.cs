using NextLedger.App.Services;
using NextLedger.App.ViewModels.Settings;
using Microsoft.UI.Xaml.Controls;

namespace NextLedger.App.Views.Settings;

public sealed partial class SettingsPage : Page
{
    private readonly SettingsViewModel _viewModel;

    public SettingsPage()
    {
        InitializeComponent();
        _viewModel = AppHost.Current.Services.GetRequiredService<SettingsViewModel>();
        DataContext = _viewModel;

        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private async void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingsViewModel.EditingAccount) && _viewModel.EditingAccount is not null)
        {
            EditAccountDialog.XamlRoot = Content.XamlRoot;
            await EditAccountDialog.ShowAsync();
        }
        else if (e.PropertyName == nameof(SettingsViewModel.EditingEnvelope) && _viewModel.EditingEnvelope is not null)
        {
            EditEnvelopeDialog.XamlRoot = Content.XamlRoot;
            await EditEnvelopeDialog.ShowAsync();
        }
    }
}
