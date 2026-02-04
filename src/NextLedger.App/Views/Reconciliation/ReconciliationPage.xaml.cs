using NextLedger.App.Services;
using NextLedger.App.ViewModels.Reconciliation;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace NextLedger.App.Views.Reconciliation;

public sealed partial class ReconciliationPage : Page
{
    public ReconciliationPage()
    {
        InitializeComponent();
        DataContext = AppHost.Current.Services.GetRequiredService<ReconciliationViewModel>();
    }

    private async void Reconcile_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ReconciliationViewModel vm)
            return;

        var selectedCount = vm.Transactions.Count(t => t.IsSelected);
        if (selectedCount == 0)
        {
            vm.ReconcileCommand.Execute(null);
            return;
        }

        ConfirmReconcileDialogText.Text = $"This will reconcile {selectedCount} selected transaction(s) for the statement month.";
        ConfirmReconcileDialog.XamlRoot = XamlRoot;
        var result = await ConfirmReconcileDialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
            vm.ReconcileCommand.Execute(null);
    }
}
