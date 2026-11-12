using ThuChiCaNhan.Models;

namespace ThuChiCaNhan.ViewModels;

public sealed class BudgetSummary
{
    public ExpenseCategory Category { get; init; } = null!;
    public decimal Limit { get; init; }
    public decimal Spent { get; init; }
    public decimal Remaining => Limit - Spent;
    public string Status => Limit <= 0 ? "Chưa đặt hạn mức" : Spent >= Limit ? "Đã vượt" : Spent >= Limit * 0.8m ? "Gần chạm" : "Trong hạn";
}
