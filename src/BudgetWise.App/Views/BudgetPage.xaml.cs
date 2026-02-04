using Microsoft.UI.Xaml.Controls;
using BudgetWise.App.Services;
using BudgetWise.App.ViewModels;

namespace BudgetWise.App.Views;

public sealed partial class BudgetPage : Page
{
    public BudgetPage()
    {
        InitializeComponent();
        DataContext = AppHost.Current.Services.GetRequiredService<BudgetViewModel>();
    }
}
