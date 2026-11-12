using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using ThuChiCaNhan.Models;
using ThuChiCaNhan.ViewModels;

namespace ThuChiCaNhan;

public partial class BudgetDialog : Window
{
    private readonly MainViewModel _viewModel;
    private bool _loading;

    public BudgetDialog(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        CategorySelector.ItemsSource = viewModel.Categories.Where(category => category.Type == "Chi tiêu").ToList();
        CategorySelector.SelectedIndex = 0;
        MonthSelector.SelectedItem = viewModel.SelectedMonth;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e) => await LoadBudgetsAsync();

    private async void MonthSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded && !_loading) await LoadBudgetsAsync();
    }

    private async Task LoadBudgetsAsync()
    {
        if (MonthSelector.SelectedItem is not MonthOption month) return;
        try
        {
            _loading = true;
            BudgetsGrid.ItemsSource = await _viewModel.GetBudgetSummariesAsync(month.StartDate);
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "Không thể tải ngân sách", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _loading = false;
        }
    }

    private void BudgetsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (BudgetsGrid.SelectedItem is not BudgetSummary summary) return;
        CategorySelector.SelectedItem = CategorySelector.Items.Cast<ExpenseCategory>()
            .FirstOrDefault(category => category.Id == summary.Category.Id);
        LimitInput.Text = summary.Limit.ToString("0.##", CultureInfo.GetCultureInfo("vi-VN"));
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (MonthSelector.SelectedItem is not MonthOption month || CategorySelector.SelectedItem is not ExpenseCategory category ||
            !decimal.TryParse(LimitInput.Text, NumberStyles.Number, CultureInfo.GetCultureInfo("vi-VN"), out var limit) || limit <= 0)
        {
            MessageBox.Show("Chọn danh mục, tháng và nhập hạn mức lớn hơn 0.", "Thông tin chưa hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            await _viewModel.SaveBudgetAsync(category.Id, month.StartDate, limit);
            await LoadBudgetsAsync();
            if (BudgetsGrid.Items.Cast<BudgetSummary>().FirstOrDefault(item => item.Category.Id == category.Id) is { } summary &&
                summary.Limit > 0 && summary.Spent >= summary.Limit * 0.8m)
            {
                var warning = summary.Spent >= summary.Limit
                    ? $"Đã vượt ngân sách {category.Name}: chi {summary.Spent:N0} ₫ trên hạn mức {summary.Limit:N0} ₫."
                    : $"Đã đạt ít nhất 80% ngân sách {category.Name}: chi {summary.Spent:N0} ₫ trên hạn mức {summary.Limit:N0} ₫.";
                MessageBox.Show(warning, "Cảnh báo ngân sách", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception exception)
        {
            MessageBox.Show($"Không thể lưu hạn mức.\n\n{exception.Message}", "Lỗi dữ liệu", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
