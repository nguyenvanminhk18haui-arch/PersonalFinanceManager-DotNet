using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using ThuChiCaNhan.Models;

namespace ThuChiCaNhan;

public partial class TransactionDialog : Window
{
    private readonly IReadOnlyList<ExpenseCategory> _categories;
    private readonly IReadOnlyList<Wallet> _wallets;
    public TransactionRecord? NewTransaction { get; private set; }

    public TransactionDialog(IEnumerable<ExpenseCategory> categories, IEnumerable<Wallet> wallets)
    {
        InitializeComponent();
        _categories = categories.ToList();
        _wallets = wallets.ToList();
        WalletSelector.ItemsSource = _wallets;
        WalletSelector.SelectedIndex = 0;
        DateInput.SelectedDate = DateTime.Today;
        UpdateCategories();
        Loaded += (_, _) => AmountInput.Focus();
    }

    private void TypeSelector_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateCategories();

    private void UpdateCategories()
    {
        if (CategorySelector is null || TypeSelector?.SelectedItem is not ComboBoxItem item) return;
        var type = item.Content?.ToString();
        CategorySelector.ItemsSource = _categories.Where(category => category.Type == type).ToList();
        CategorySelector.SelectedIndex = 0;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var culture = CultureInfo.GetCultureInfo("vi-VN");
        if (!decimal.TryParse(AmountInput.Text, NumberStyles.Number, culture, out var amount) || amount <= 0)
        {
            MessageBox.Show("Nhập số tiền lớn hơn 0, ví dụ: 85000.", "Số tiền chưa hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
            AmountInput.Focus();
            return;
        }

        if (CategorySelector.SelectedItem is not ExpenseCategory category ||
            WalletSelector.SelectedItem is not Wallet wallet ||
            DateInput.SelectedDate is not DateTime date)
        {
            MessageBox.Show("Hãy chọn danh mục, ví và ngày giao dịch.", "Thông tin chưa đầy đủ", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var type = (TypeSelector.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Chi tiêu";
        NewTransaction = new TransactionRecord
        {
            Amount = decimal.Round(amount, 2),
            Type = type,
            CategoryId = category.Id,
            Category = category,
            WalletId = wallet.Id,
            Wallet = wallet,
            Note = NoteInput.Text.Trim(),
            Date = date.Date
        };
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
