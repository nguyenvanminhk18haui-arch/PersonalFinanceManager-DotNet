namespace ThuChiCaNhan.Models;

public sealed class ExpenseCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public ICollection<TransactionRecord> Transactions { get; set; } = new List<TransactionRecord>();
}
