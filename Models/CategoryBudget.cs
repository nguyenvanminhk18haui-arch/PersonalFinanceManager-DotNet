namespace ThuChiCaNhan.Models;

public sealed class CategoryBudget
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public ExpenseCategory Category { get; set; } = null!;
    public DateTime Month { get; set; }
    public decimal Limit { get; set; }
}
