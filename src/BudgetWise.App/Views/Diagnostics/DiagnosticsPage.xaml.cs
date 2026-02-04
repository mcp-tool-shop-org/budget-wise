using BudgetWise.App.Services;
using BudgetWise.App.ViewModels.Diagnostics;
using Microsoft.UI.Xaml.Controls;

namespace BudgetWise.App.Views.Diagnostics;

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
