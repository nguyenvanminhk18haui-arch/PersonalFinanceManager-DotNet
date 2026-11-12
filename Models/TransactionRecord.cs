namespace ThuChiCaNhan.Models;

public sealed class TransactionRecord
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public int CategoryId { get; set; }
    public ExpenseCategory Category { get; set; } = null!;
    public int WalletId { get; set; }
    public Wallet Wallet { get; set; } = null!;
    public int? RecurringTransactionId { get; set; }
    public RecurringTransaction? RecurringTransaction { get; set; }
}
