using ClosedXML.Excel;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ThuChiCaNhan.Models;
using ThuChiCaNhan.ViewModels;

namespace ThuChiCaNhan;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel = new();
    private bool _initialized;
    private bool _suppressMonthRefresh;
    private readonly DispatcherTimer _recurringTimer = new() { Interval = TimeSpan.FromHours(1) };

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;
        _recurringTimer.Tick += RecurringTimer_Tick;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await _viewModel.InitializeAsync();
            _initialized = true;
            _recurringTimer.Start();
        }
        catch (Exception exception)
        {
            _viewModel.StatusMessage = "Không thể kết nối cơ sở dữ liệu. Hãy kiểm tra SQL Server LocalDB.";
            MessageBox.Show(
                $"Ứng dụng chưa kết nối được SQL Server. Hãy cài SQL Server LocalDB hoặc đặt biến môi trường THUCHI_SQL_CONNECTION.\n\nChi tiết: {exception.GetBaseException().Message}",
                "Không thể mở sổ thu chi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void RecurringTimer_Tick(object? sender, EventArgs e)
    {
        try
        {
            await _viewModel.RunDueRecurringAsync();
        }
        catch (Exception exception)
        {
            _viewModel.StatusMessage = $"Không thể xử lý giao dịch định kỳ: {exception.Message}";
        }
    }

    private async void MonthSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_initialized || _suppressMonthRefresh || MonthSelector.SelectedItem is not MonthOption) return;
        try
        {
            await _viewModel.RefreshAsync();
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "Không thể tải giao dịch", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void AddTransaction_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new TransactionDialog(_viewModel.Categories, _viewModel.Wallets) { Owner = this };
        if (dialog.ShowDialog() != true || dialog.NewTransaction is null) return;

        try
        {
            var transaction = dialog.NewTransaction;
            await _viewModel.AddTransactionAsync(transaction);
            var month = _viewModel.GetOrAddMonth(transaction.Date);
            if (_viewModel.SelectedMonth?.StartDate != month.StartDate)
            {
                _suppressMonthRefresh = true;
                _viewModel.SelectedMonth = month;
                _suppressMonthRefresh = false;
            }
            await _viewModel.RefreshAsync();
            _viewModel.StatusMessage = "Đã lưu giao dịch.";
            var warning = await _viewModel.GetTransactionBudgetWarningAsync(transaction);
            if (warning is not null)
            {
                _viewModel.StatusMessage = warning;
                MessageBox.Show(warning, "Cảnh báo ngân sách", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception exception)
        {
            MessageBox.Show($"Không thể lưu giao dịch.\n\nChi tiết SQL: {exception.GetBaseException().Message}", "Lỗi lưu dữ liệu", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void DeleteTransaction_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: int id }) return;
        if (MessageBox.Show("Xóa giao dịch này khỏi sổ?", "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        try
        {
            await _viewModel.DeleteTransactionAsync(id);
        }
        catch (Exception exception)
        {
            MessageBox.Show($"Không thể xóa giao dịch.\n\n{exception.Message}", "Lỗi dữ liệu", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void Export_Click(object sender, RoutedEventArgs e)
    {
        var month = _viewModel.SelectedMonth;
        if (month is null) return;

        var picker = new SaveFileDialog
        {
            Title = "Lưu báo cáo thu chi",
            FileName = $"So-thu-chi-{month.StartDate:yyyy-MM}.xlsx",
            Filter = "Excel Workbook (*.xlsx)|*.xlsx",
            DefaultExt = ".xlsx",
            AddExtension = true
        };
        if (picker.ShowDialog(this) != true) return;

        try
        {
            var rows = await _viewModel.GetSelectedMonthTransactionsAsync();
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Báo cáo tháng");
            sheet.Cell("A1").Value = "SỔ NHỎ · BÁO CÁO THU CHI";
            sheet.Range("A1:F1").Merge();
            sheet.Cell("A2").Value = $"Tháng {month.StartDate:MM/yyyy}";
            sheet.Range("A2:F2").Merge();
            sheet.Cell("A4").Value = "Ngày";
            sheet.Cell("B4").Value = "Loại";
            sheet.Cell("C4").Value = "Danh mục";
            sheet.Cell("D4").Value = "Ví";
            sheet.Cell("E4").Value = "Ghi chú";
            sheet.Cell("F4").Value = "Số tiền (VND)";
            for (var index = 0; index < rows.Count; index++)
            {
                var row = index + 5;
                sheet.Cell(row, 1).Value = rows[index].Date;
                sheet.Cell(row, 1).Style.DateFormat.Format = "dd/mm/yyyy";
                sheet.Cell(row, 2).Value = rows[index].Type;
                sheet.Cell(row, 3).Value = rows[index].Category.Name;
                sheet.Cell(row, 4).Value = rows[index].Wallet.Name;
                sheet.Cell(row, 5).Value = rows[index].Note;
                sheet.Cell(row, 6).Value = rows[index].Amount;
                sheet.Cell(row, 6).Style.NumberFormat.Format = "#,##0";
            }

            var incomeRow = rows.Count + 6;
            var expenseRow = incomeRow + 1;
            var balanceRow = expenseRow + 1;
            var lastDataRow = rows.Count + 4;
            sheet.Cell(incomeRow, 5).Value = "TỔNG THU NHẬP";
            sheet.Cell(expenseRow, 5).Value = "TỔNG CHI TIÊU";
            sheet.Cell(balanceRow, 5).Value = "SỐ DƯ THÁNG";
            sheet.Cell(incomeRow, 6).FormulaA1 = rows.Count == 0 ? "=0" : $"=SUMIF(B5:B{lastDataRow},\"Thu nhập\",F5:F{lastDataRow})";
            sheet.Cell(expenseRow, 6).FormulaA1 = rows.Count == 0 ? "=0" : $"=SUMIF(B5:B{lastDataRow},\"Chi tiêu\",F5:F{lastDataRow})";
            sheet.Cell(balanceRow, 6).FormulaA1 = $"=F{incomeRow}-F{expenseRow}";
            sheet.Range(incomeRow, 6, balanceRow, 6).Style.NumberFormat.Format = "#,##0";
            sheet.Range(1, 1, balanceRow, 6).Style.Font.FontName = "Aptos";
            sheet.Cell("A1").Style.Font.Bold = true;
            sheet.Cell("A1").Style.Font.FontSize = 16;
            sheet.Cell("A1").Style.Font.FontColor = XLColor.FromHtml("#152C42");
            sheet.Cell("A2").Style.Font.FontColor = XLColor.FromHtml("#75839A");
            sheet.Range("A4:F4").Style.Font.Bold = true;
            sheet.Range("A4:F4").Style.Fill.BackgroundColor = XLColor.FromHtml("#E8F4F1");
            sheet.Range(incomeRow, 5, balanceRow, 6).Style.Font.Bold = true;
            sheet.Range(incomeRow, 5, balanceRow, 6).Style.Fill.BackgroundColor = XLColor.FromHtml("#F3F6FA");
            sheet.Columns(1, 6).AdjustToContents();
            sheet.Column(5).Width = Math.Max(sheet.Column(5).Width, 24);
            sheet.SheetView.FreezeRows(4);
            workbook.SaveAs(picker.FileName);
            _viewModel.StatusMessage = "Đã xuất báo cáo Excel.";
            MessageBox.Show("Báo cáo tháng đã được lưu.", "Xuất Excel thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception exception)
        {
            MessageBox.Show($"Không thể xuất báo cáo.\n\n{exception.Message}", "Lỗi xuất Excel", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OverviewNav_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.StatusMessage = "Đang xem tổng quan tài chính.";
        MonthSelector.Focus();
    }

    private void TransactionsNav_Click(object sender, RoutedEventArgs e)
    {
        TransactionsGrid.BringIntoView();
        TransactionsGrid.Focus();
        _viewModel.StatusMessage = $"Đang xem giao dịch tháng {_viewModel.SelectedMonth}.";
    }

    private void CategoriesNav_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new CategoryDialog(_viewModel.Categories) { Owner = this };
        dialog.ShowDialog();
    }

    private void BudgetsNav_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new BudgetDialog(_viewModel) { Owner = this };
        dialog.ShowDialog();
    }

    private void WalletsNav_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new WalletDialog(_viewModel) { Owner = this };
        dialog.ShowDialog();
    }

    private void RecurringNav_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new RecurringDialog(_viewModel) { Owner = this };
        dialog.ShowDialog();
    }

    private void ReportsNav_Click(object sender, RoutedEventArgs e) => Export_Click(sender, e);
}
