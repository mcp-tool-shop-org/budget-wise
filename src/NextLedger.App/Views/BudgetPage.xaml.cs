using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using NextLedger.App.Services;
using NextLedger.App.ViewModels;
using Windows.System;
using Windows.UI;

namespace NextLedger.App.Views;

public sealed partial class BudgetPage : Page
{
    private static readonly Brush HoverBrush = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255));

    public BudgetPage()
    {
        InitializeComponent();
        DataContext = AppHost.Current.Services.GetRequiredService<BudgetViewModel>();
    }

    private async void AllocationTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (sender is not TextBox textBox || textBox.DataContext is not EnvelopeRowViewModel row)
            return;

        if (e.Key == VirtualKey.Enter)
        {
            e.Handled = true;
            await row.CommitAllocationCommand.ExecuteAsync(null);
            // Move focus away to show the change took effect
            FocusManager.TryMoveFocus(FocusNavigationDirection.Next);
        }
        else if (e.Key == VirtualKey.Escape)
        {
            e.Handled = true;
            row.CancelEditCommand.Execute(null);
            FocusManager.TryMoveFocus(FocusNavigationDirection.Next);
        }
    }

    private async void AllocationTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBox textBox || textBox.DataContext is not EnvelopeRowViewModel row)
            return;

        // Auto-commit on focus loss if there's a pending change
        if (row.HasPendingChange)
        {
            await row.CommitAllocationCommand.ExecuteAsync(null);
        }
    }

    private void EnvelopeRow_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Grid grid)
        {
            grid.Background = HoverBrush;
        }
    }

    private void EnvelopeRow_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Grid grid)
        {
            // Reset to theme resource
            grid.Background = (Brush)Microsoft.UI.Xaml.Application.Current.Resources["CardBackgroundFillColorSecondaryBrush"];
        }
    }
}
