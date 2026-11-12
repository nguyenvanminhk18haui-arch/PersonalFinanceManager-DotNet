using System.Windows;
using System.Windows.Controls;
using ThuChiCaNhan.ViewModels;

namespace ThuChiCaNhan;

public partial class WalletDialog : Window
{
    private readonly MainViewModel _viewModel;

    public WalletDialog(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e) => await LoadWalletsAsync();

    private async Task LoadWalletsAsync()
    {
        try
        {
            WalletsGrid.ItemsSource = await _viewModel.GetWalletSummariesAsync();
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "Không thể tải ví", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void Add_Click(object sender, RoutedEventArgs e)
    {
        if (TypeSelector.SelectedItem is not ComboBoxItem typeItem)
            return;
        try
        {
            await _viewModel.AddWalletAsync(NameInput.Text, typeItem.Content?.ToString() ?? "Tiền mặt");
            NameInput.Clear();
            await LoadWalletsAsync();
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "Không thể thêm ví", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
