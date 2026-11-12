using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using Microsoft.EntityFrameworkCore;
using ThuChiCaNhan.Data;
using ThuChiCaNhan.Models;

namespace ThuChiCaNhan.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    public ObservableCollection<TransactionRecord> Transactions { get; } = [];
    public ObservableCollection<ExpenseCategory> Categories { get; } = [];
    public ObservableCollection<Wallet> Wallets { get; } = [];
    public ObservableCollection<MonthOption> MonthOptions { get; } = [];
    public ObservableCollection<ISeries> ExpenseSeries { get; } = [];

    private MonthOption? _selectedMonth;
    private decimal _income;
    private decimal _expense;
    private decimal _balance;
    private string _statusMessage = "Sẵn sàng ghi lại một ngày mới.";
    private string _budgetAlert = string.Empty;

    public MonthOption? SelectedMonth
    {
        get => _selectedMonth;
        set => SetField(ref _selectedMonth, value);
    }

    public decimal Income { get => _income; private set => SetField(ref _income, value); }
    public decimal Expense { get => _expense; private set => SetField(ref _expense, value); }
    public decimal Balance { get => _balance; private set => SetField(ref _balance, value); }
    public string StatusMessage { get => _statusMessage; set => SetField(ref _statusMessage, value); }
    public string BudgetAlert { get => _budgetAlert; private set => SetField(ref _budgetAlert, value); }

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainViewModel()
    {
        var today = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        for (var offset = 0; offset < 12; offset++)
            MonthOptions.Add(new MonthOption(today.AddMonths(-offset)));
        SelectedMonth = MonthOptions[0];
    }

    public async Task InitializeAsync()
    {
        await using var db = new TrackerDbContext();
        await DbInitializer.InitializeAsync(db);
        Categories.Clear();
        foreach (var category in await db.Categories.OrderBy(category => category.Type).ThenBy(category => category.Name).ToListAsync())
            Categories.Add(category);
        await RefreshWalletsAsync();
        var createdCount = await ProcessDueRecurringAsync();
        await RefreshAsync();
        if (createdCount > 0)
            StatusMessage = $"Đã tự động ghi nhận {createdCount} giao dịch định kỳ đến hạn.";
    }

    public async Task RefreshAsync()
    {
        if (SelectedMonth is null) return;

        await using var db = new TrackerDbContext();
        var start = SelectedMonth.StartDate;
        var end = start.AddMonths(1);
        var rows = await db.Transactions
            .Include(transaction => transaction.Category)
            .Include(transaction => transaction.Wallet)
            .Where(transaction => transaction.Date >= start && transaction.Date < end)
            .OrderByDescending(transaction => transaction.Date)
            .ThenByDescending(transaction => transaction.Id)
            .ToListAsync();

        Transactions.Clear();
        foreach (var row in rows) Transactions.Add(row);
        Income = rows.Where(row => row.Type == "Thu nhập").Sum(row => row.Amount);
        Expense = rows.Where(row => row.Type == "Chi tiêu").Sum(row => row.Amount);
        Balance = Income - Expense;
        var budgetSummaries = await GetBudgetSummariesAsync(start, db);
        var warnings = budgetSummaries
            .Where(budget => budget.Limit > 0 && budget.Spent >= budget.Limit * 0.8m)
            .Select(budget => $"{budget.Category.Name}: {budget.Spent:N0}/{budget.Limit:N0} ₫ ({budget.Spent / budget.Limit:P0})")
            .ToList();
        BudgetAlert = warnings.Count == 0
            ? string.Empty
            : $"Cảnh báo ngân sách · {string.Join("  |  ", warnings)}";

        ExpenseSeries.Clear();
        var palette = new[] { "#168F78", "#F2A65A", "#6679C7", "#E47770", "#49A6B0", "#8D79B6", "#9AA6B2", "#D6B84C" };
        var categoryTotals = rows.Where(row => row.Type == "Chi tiêu")
            .GroupBy(row => row.Category.Name)
            .Select(group => new { Name = group.Key, Total = (double)group.Sum(row => row.Amount) })
            .OrderByDescending(group => group.Total)
            .ToList();

        for (var index = 0; index < categoryTotals.Count; index++)
        {
            ExpenseSeries.Add(new PieSeries<double>
            {
                Name = categoryTotals[index].Name,
                Values = [categoryTotals[index].Total],
                Fill = new LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(
                    SkiaSharp.SKColor.Parse(palette[index % palette.Length]))
            });
        }

        StatusMessage = rows.Count == 0
            ? "Chưa có giao dịch trong tháng này. Hãy thêm khoản đầu tiên."
            : $"Đang xem {rows.Count} giao dịch trong tháng {SelectedMonth}.";
    }

    public async Task AddTransactionAsync(TransactionRecord transaction)
    {
        await using var db = new TrackerDbContext();
        db.Categories.Attach(transaction.Category);
        db.Wallets.Attach(transaction.Wallet);
        db.Transactions.Add(transaction);
        await db.SaveChangesAsync();
        StatusMessage = "Đã lưu giao dịch.";
    }

    public MonthOption GetOrAddMonth(DateTime date)
    {
        var month = new DateTime(date.Year, date.Month, 1);
        var existing = MonthOptions.FirstOrDefault(option => option.StartDate == month);
        if (existing is not null) return existing;

        var added = new MonthOption(month);
        MonthOptions.Add(added);
        return added;
    }

    public async Task<List<BudgetSummary>> GetBudgetSummariesAsync(DateTime month, TrackerDbContext? existingContext = null)
    {
        if (existingContext is not null)
            return await LoadBudgetSummariesAsync(existingContext, month);

        await using var db = new TrackerDbContext();
        return await LoadBudgetSummariesAsync(db, month);
    }

    private static async Task<List<BudgetSummary>> LoadBudgetSummariesAsync(TrackerDbContext db, DateTime month)
    {
        var start = new DateTime(month.Year, month.Month, 1);
        var end = start.AddMonths(1);
        var budgets = await db.Budgets.Include(budget => budget.Category)
            .Where(budget => budget.Month == start)
            .ToListAsync();
        var spent = await db.Transactions
            .Where(transaction => transaction.Type == "Chi tiêu" && transaction.Date >= start && transaction.Date < end)
            .GroupBy(transaction => transaction.CategoryId)
            .Select(group => new { CategoryId = group.Key, Total = group.Sum(transaction => transaction.Amount) })
            .ToDictionaryAsync(group => group.CategoryId, group => group.Total);

        return budgets.Select(budget => new BudgetSummary
        {
            Category = budget.Category,
            Limit = budget.Limit,
            Spent = spent.GetValueOrDefault(budget.CategoryId)
        }).OrderBy(budget => budget.Category.Name).ToList();
    }

    public async Task SaveBudgetAsync(int categoryId, DateTime month, decimal limit)
    {
        await using var db = new TrackerDbContext();
        var start = new DateTime(month.Year, month.Month, 1);
        var budget = await db.Budgets.SingleOrDefaultAsync(item => item.CategoryId == categoryId && item.Month == start);
        if (budget is null)
            db.Budgets.Add(new CategoryBudget { CategoryId = categoryId, Month = start, Limit = limit });
        else
            budget.Limit = limit;
        await db.SaveChangesAsync();
        if (SelectedMonth?.StartDate == start) await RefreshAsync();
    }

    public async Task RefreshWalletsAsync()
    {
        await using var db = new TrackerDbContext();
        var wallets = await db.Wallets.OrderBy(wallet => wallet.Name).ToListAsync();
        Wallets.Clear();
        foreach (var wallet in wallets) Wallets.Add(wallet);
    }

    public async Task AddWalletAsync(string name, string type)
    {
        var normalizedName = name.Trim();
        if (normalizedName.Length == 0 || normalizedName.Length > 80) throw new InvalidOperationException("Tên ví phải từ 1 đến 80 ký tự.");
        await using var db = new TrackerDbContext();
        if (await db.Wallets.AnyAsync(wallet => wallet.Name == normalizedName))
            throw new InvalidOperationException("Tên ví này đã tồn tại.");
        db.Wallets.Add(new Wallet { Name = normalizedName, Type = type });
        await db.SaveChangesAsync();
        await RefreshWalletsAsync();
    }

    public async Task<List<WalletSummary>> GetWalletSummariesAsync()
    {
        await using var db = new TrackerDbContext();
        var wallets = await db.Wallets.OrderBy(wallet => wallet.Name).ToListAsync();
        var totals = await db.Transactions.GroupBy(transaction => transaction.WalletId)
            .Select(group => new
            {
                WalletId = group.Key,
                Balance = group.Sum(transaction => transaction.Type == "Thu nhập" ? transaction.Amount : -transaction.Amount)
            }).ToDictionaryAsync(group => group.WalletId, group => group.Balance);
        return wallets.Select(wallet => new WalletSummary
        {
            Wallet = wallet,
            Balance = totals.GetValueOrDefault(wallet.Id)
        }).ToList();
    }

    public async Task<List<RecurringTransaction>> GetRecurringTransactionsAsync()
    {
        await using var db = new TrackerDbContext();
        return await db.RecurringTransactions.Include(schedule => schedule.Category)
            .Include(schedule => schedule.Wallet)
            .OrderBy(schedule => schedule.NextDueDate)
            .ToListAsync();
    }

    public async Task AddRecurringTransactionAsync(RecurringTransaction schedule)
    {
        await using var db = new TrackerDbContext();
        db.Categories.Attach(schedule.Category);
        db.Wallets.Attach(schedule.Wallet);
        db.RecurringTransactions.Add(schedule);
        await db.SaveChangesAsync();
        var createdCount = await ProcessDueRecurringAsync();
        await RefreshAsync();
        if (createdCount > 0) StatusMessage = $"Đã lưu lịch định kỳ và tự động ghi {createdCount} giao dịch đến hạn.";
    }

    public async Task DeleteRecurringTransactionAsync(int id)
    {
        await using var db = new TrackerDbContext();
        var schedule = await db.RecurringTransactions.FindAsync(id);
        if (schedule is null) return;
        db.RecurringTransactions.Remove(schedule);
        await db.SaveChangesAsync();
    }

    private static async Task<int> ProcessDueRecurringAsync()
    {
        await using var db = new TrackerDbContext();
        var today = DateTime.Today;
        var schedules = await db.RecurringTransactions.Where(schedule => schedule.IsActive && schedule.NextDueDate <= today)
            .OrderBy(schedule => schedule.NextDueDate).ToListAsync();
        var createdCount = 0;
        foreach (var schedule in schedules)
        {
            while (schedule.NextDueDate.Date <= today)
            {
                db.Transactions.Add(new TransactionRecord
                {
                    Amount = schedule.Amount,
                    Type = schedule.Type,
                    Note = schedule.Note,
                    Date = schedule.NextDueDate.Date,
                    CategoryId = schedule.CategoryId,
                    WalletId = schedule.WalletId,
                    RecurringTransactionId = schedule.Id
                });
                createdCount++;
                var nextMonth = new DateTime(schedule.NextDueDate.Year, schedule.NextDueDate.Month, 1).AddMonths(1);
                var day = Math.Min(schedule.DayOfMonth, DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month));
                schedule.NextDueDate = new DateTime(nextMonth.Year, nextMonth.Month, day);
            }
        }

        if (createdCount > 0) await db.SaveChangesAsync();
        return createdCount;
    }

    public async Task<int> RunDueRecurringAsync()
    {
        var createdCount = await ProcessDueRecurringAsync();
        if (createdCount == 0) return 0;
        await RefreshAsync();
        StatusMessage = $"Đã tự động ghi nhận {createdCount} giao dịch định kỳ đến hạn.";
        return createdCount;
    }

    public async Task<string?> GetTransactionBudgetWarningAsync(TransactionRecord transaction)
    {
        if (transaction.Type != "Chi tiêu" || SelectedMonth is null) return null;
        var budget = (await GetBudgetSummariesAsync(transaction.Date))
            .FirstOrDefault(item => item.Category.Id == transaction.CategoryId);
        if (budget is null || budget.Limit <= 0 || budget.Spent < budget.Limit * 0.8m) return null;
        return budget.Spent >= budget.Limit
            ? $"Đã vượt ngân sách {budget.Category.Name}: chi {budget.Spent:N0} ₫ trên hạn mức {budget.Limit:N0} ₫."
            : $"Sắp chạm ngân sách {budget.Category.Name}: đã chi {budget.Spent:N0} ₫ trên hạn mức {budget.Limit:N0} ₫.";
    }

    public async Task DeleteTransactionAsync(int id)
    {
        await using var db = new TrackerDbContext();
        var record = await db.Transactions.FindAsync(id);
        if (record is null) return;
        db.Transactions.Remove(record);
        await db.SaveChangesAsync();
        await RefreshAsync();
        StatusMessage = "Đã xóa giao dịch.";
    }

    public async Task<List<TransactionRecord>> GetSelectedMonthTransactionsAsync()
    {
        if (SelectedMonth is null) return [];
        await using var db = new TrackerDbContext();
        var start = SelectedMonth.StartDate;
        return await db.Transactions.Include(transaction => transaction.Category)
            .Where(transaction => transaction.Date >= start && transaction.Date < start.AddMonths(1))
            .OrderBy(transaction => transaction.Date)
            .ThenBy(transaction => transaction.Id)
            .ToListAsync();
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        OnPropertyChanged(propertyName);
    }
}
