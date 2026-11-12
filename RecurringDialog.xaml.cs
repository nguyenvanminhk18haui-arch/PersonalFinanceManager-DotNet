using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using ThuChiCaNhan.Models;
using ThuChiCaNhan.ViewModels;

namespace ThuChiCaNhan;

public partial class RecurringDialog : Window
{
    private readonly MainViewModel _viewModel;

    public RecurringDialog(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        CategorySelector.ItemsSource = ExpenseCategories();
        WalletSelector.ItemsSource = viewModel.Wallets;
        WalletSelector.SelectedIndex = 0;
        DateInput.SelectedDate = DateTime.Today;
    }

    private List<ExpenseCategory> ExpenseCategories() => _viewModel.Categories.Where(category => category.Type == "Chi tiêu").ToList();

    private async void Window_Loaded(object sender, RoutedEventArgs e) => await ReloadAsync();

    private void TypeSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CategorySelector is null || TypeSelector.SelectedItem is not ComboBoxItem item) return;
        var type = item.Content?.ToString();
        CategorySelector.ItemsSource = _viewModel.Categories.Where(category => category.Type == type).ToList();
        CategorySelector.SelectedIndex = 0;
    }

    private async Task ReloadAsync()
    {
        try
        {
            SchedulesGrid.ItemsSource = await _viewModel.GetRecurringTransactionsAsync();
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "Không thể tải lịch định kỳ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void Add_Click(object sender, RoutedEventArgs e)
    {
        if (!decimal.TryParse(AmountInput.Text, NumberStyles.Number, CultureInfo.GetCultureInfo("vi-VN"), out var amount) || amount <= 0 ||
            CategorySelector.SelectedItem is not ExpenseCategory category || WalletSelector.SelectedItem is not Wallet wallet ||
            DateInput.SelectedDate is not DateTime dueDate || TypeSelector.SelectedItem is not ComboBoxItem typeItem)
        {
            MessageBox.Show("Hãy nhập số tiền hợp lệ và chọn loại, danh mục, ví, ngày đến hạn.", "Thông tin chưa hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var schedule = new RecurringTransaction
        {
            Amount = decimal.Round(amount, 2),
            Type = typeItem.Content?.ToString() ?? "Chi tiêu",
            Note = NoteInput.Text.Trim(),
            CategoryId = category.Id,
            Category = category,
            WalletId = wallet.Id,
            Wallet = wallet,
            NextDueDate = dueDate.Date,
            DayOfMonth = dueDate.Day,
            IsActive = true
        };

        try
        {
            await _viewModel.AddRecurringTransactionAsync(schedule);
            AmountInput.Clear();
            NoteInput.Clear();
            await ReloadAsync();
        }
        catch (Exception exception)
        {
            MessageBox.Show($"Không thể tạo lịch định kỳ.\n\n{exception.Message}", "Lỗi dữ liệu", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: int id } ||
            MessageBox.Show("Xóa lịch định kỳ này? Các giao dịch đã ghi trước đó sẽ được giữ nguyên.", "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;
        try
        {
            await _viewModel.DeleteRecurringTransactionAsync(id);
            await ReloadAsync();
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "Không thể xóa lịch", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
