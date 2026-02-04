using NextLedger.App.Services;
using NextLedger.App.ViewModels.Transactions;
using Microsoft.UI.Xaml.Controls;

namespace NextLedger.App.Views.Transactions;

public sealed partial class TransactionsPage : Page
{
    public TransactionsPage()
    {
        InitializeComponent();
        DataContext = AppHost.Current.Services.GetRequiredService<TransactionsViewModel>();
    }
}
