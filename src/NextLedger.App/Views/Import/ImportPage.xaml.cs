using System.Linq;
using NextLedger.Application.DTOs;
using NextLedger.App.Services;
using NextLedger.App.ViewModels.Import;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace NextLedger.App.Views.Import;

public sealed partial class ImportPage : Page
{
    public ImportPage()
    {
        InitializeComponent();
        DataContext = AppHost.Current.Services.GetRequiredService<ImportViewModel>();
    }

    private async void PickCsv_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ImportViewModel vm)
            return;

        var picker = new FileOpenPicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            ViewMode = PickerViewMode.List
        };

        picker.FileTypeFilter.Add(".csv");
        picker.FileTypeFilter.Add(".txt");

        // WinUI 3 desktop apps must initialize the picker with a window handle.
        var window = AppHost.Current.Services.GetRequiredService<MainWindow>();
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        StorageFile? file = await picker.PickSingleFileAsync();
        if (file is null)
            return;

        vm.CsvText = await FileIO.ReadTextAsync(file);
    }

    private async void ImportSelected_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ImportViewModel vm)
            return;

        var selectedCount = vm.Rows.Count(r => r.IsSelected && r.Status == CsvImportRowStatus.New && r.CanCommit);
        if (selectedCount == 0)
        {
            vm.CommitCommand.Execute(null);
            return;
        }

        ConfirmImportDialogText.Text = $"This will import {selectedCount} new row(s) into the selected account.";
        ConfirmImportDialog.XamlRoot = XamlRoot;
        var result = await ConfirmImportDialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
            vm.CommitCommand.Execute(null);
    }
}
