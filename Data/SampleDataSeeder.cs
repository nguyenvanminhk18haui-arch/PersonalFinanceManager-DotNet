using System.IO;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ThuChiCaNhan.Models;

namespace ThuChiCaNhan.Data;

internal static class SampleDataSeeder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task SeedIfEmptyAsync(TrackerDbContext db)
    {
        if (await db.Transactions.AnyAsync() ||
            await db.Budgets.AnyAsync() ||
            await db.RecurringTransactions.AnyAsync())
            return;

        await using var resource = typeof(SampleDataSeeder).Assembly
            .GetManifestResourceStream("ThuChiCaNhan.Data.sample-data.json")
            ?? throw new InvalidOperationException("Không tìm thấy bộ dữ liệu mẫu được đóng gói cùng ứng dụng.");
        var sample = await JsonSerializer.DeserializeAsync<SampleData>(resource, JsonOptions)
            ?? throw new InvalidDataException("Bộ dữ liệu mẫu không hợp lệ.");

        var categories = new Dictionary<(string Name, string Type), ExpenseCategory>();
        foreach (var item in sample.Categories)
        {
            var category = await db.Categories.SingleOrDefaultAsync(row => row.Name == item.Name && row.Type == item.Type);
            if (category is null)
            {
                category = new ExpenseCategory { Name = item.Name, Type = item.Type };
                db.Categories.Add(category);
            }
            categories[(item.Name, item.Type)] = category;
        }

        var wallets = new Dictionary<string, Wallet>();
        foreach (var item in sample.Wallets)
        {
            var wallet = await db.Wallets.SingleOrDefaultAsync(row => row.Name == item.Name);
            if (wallet is null)
            {
                wallet = new Wallet { Name = item.Name, Type = item.Type };
                db.Wallets.Add(wallet);
            }
            wallets[item.Name] = wallet;
        }

        await db.SaveChangesAsync();

        foreach (var item in sample.Budgets)
        {
            db.Budgets.Add(new CategoryBudget
            {
                Category = categories[(item.CategoryName, item.CategoryType)],
                Month = item.Month,
                Limit = item.Limit
            });
        }

        foreach (var item in sample.RecurringTransactions)
        {
            db.RecurringTransactions.Add(new RecurringTransaction
            {
                Amount = item.Amount,
                Type = item.Type,
                Note = item.Note,
                Category = categories[(item.CategoryName, item.CategoryType)],
                Wallet = wallets[item.WalletName],
                NextDueDate = item.NextDueDate,
                DayOfMonth = item.DayOfMonth,
                IsActive = item.IsActive
            });
        }

        foreach (var item in sample.Transactions)
        {
            db.Transactions.Add(new TransactionRecord
            {
                Amount = item.Amount,
                Type = item.Type,
                Note = item.Note,
                Date = item.Date,
                Category = categories[(item.CategoryName, item.CategoryType)],
                Wallet = wallets[item.WalletName]
            });
        }

        await db.SaveChangesAsync();
    }

    private sealed class SampleData
    {
        public List<SampleCategory> Categories { get; init; } = [];
        public List<SampleWallet> Wallets { get; init; } = [];
        public List<SampleTransaction> Transactions { get; init; } = [];
        public List<SampleBudget> Budgets { get; init; } = [];
        public List<SampleRecurringTransaction> RecurringTransactions { get; init; } = [];
    }

    private sealed class SampleCategory
    {
        public string Name { get; init; } = string.Empty;
        public string Type { get; init; } = string.Empty;
    }

    private sealed class SampleWallet
    {
        public string Name { get; init; } = string.Empty;
        public string Type { get; init; } = string.Empty;
    }

    private sealed class SampleTransaction
    {
        public decimal Amount { get; init; }
        public string Type { get; init; } = string.Empty;
        public string Note { get; init; } = string.Empty;
        public DateTime Date { get; init; }
        public string CategoryName { get; init; } = string.Empty;
        public string CategoryType { get; init; } = string.Empty;
        public string WalletName { get; init; } = string.Empty;
    }

    private sealed class SampleBudget
    {
        public string CategoryName { get; init; } = string.Empty;
        public string CategoryType { get; init; } = string.Empty;
        public DateTime Month { get; init; }
        public decimal Limit { get; init; }
    }

    private sealed class SampleRecurringTransaction
    {
        public decimal Amount { get; init; }
        public string Type { get; init; } = string.Empty;
        public string Note { get; init; } = string.Empty;
        public string CategoryName { get; init; } = string.Empty;
        public string CategoryType { get; init; } = string.Empty;
        public string WalletName { get; init; } = string.Empty;
        public DateTime NextDueDate { get; init; }
        public int DayOfMonth { get; init; }
        public bool IsActive { get; init; }
    }
}
