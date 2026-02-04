using BudgetWise.App.Services;
using BudgetWise.App.ViewModels.Transactions;
using Microsoft.UI.Xaml.Controls;

namespace BudgetWise.App.Views.Transactions;

public sealed partial class TransactionsPage : Page
{
    public TransactionsPage()
    {
        InitializeComponent();
        DataContext = AppHost.Current.Services.GetRequiredService<TransactionsViewModel>();
    }
}
