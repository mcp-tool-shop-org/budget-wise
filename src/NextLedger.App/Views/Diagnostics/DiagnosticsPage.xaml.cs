using NextLedger.App.Services;
using NextLedger.App.ViewModels.Diagnostics;
using Microsoft.UI.Xaml.Controls;

namespace NextLedger.App.Views.Diagnostics;

public sealed partial class DiagnosticsPage : Page
{
    public DiagnosticsPage()
    {
        InitializeComponent();
        var vm = AppHost.Current.Services.GetRequiredService<DiagnosticsViewModel>();
        DataContext = vm;
        vm.RefreshCommand.Execute(null);
    }
}
