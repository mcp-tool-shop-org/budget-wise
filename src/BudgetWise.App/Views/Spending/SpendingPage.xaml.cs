using BudgetWise.App.Services;
using BudgetWise.App.ViewModels.Spending;
using Microsoft.UI.Xaml.Controls;

namespace BudgetWise.App.Views.Spending;

public sealed partial class SpendingPage : Page
{
    public SpendingPage()
    {
        InitializeComponent();
        DataContext = AppHost.Current.Services.GetRequiredService<SpendingViewModel>();
    }
}
