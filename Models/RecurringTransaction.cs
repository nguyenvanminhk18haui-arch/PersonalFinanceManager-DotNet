namespace ThuChiCaNhan.Models;

public sealed class RecurringTransaction
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public ExpenseCategory Category { get; set; } = null!;
    public int WalletId { get; set; }
    public Wallet Wallet { get; set; } = null!;
    public DateTime NextDueDate { get; set; }
    public int DayOfMonth { get; set; }
    public bool IsActive { get; set; } = true;
}
