using NextLedger.App.Services;
using NextLedger.App.ViewModels.Spending;
using Microsoft.UI.Xaml.Controls;

namespace NextLedger.App.Views.Spending;

public sealed partial class SpendingPage : Page
{
    public SpendingPage()
    {
        InitializeComponent();
        DataContext = AppHost.Current.Services.GetRequiredService<SpendingViewModel>();
    }
}
