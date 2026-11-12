namespace ThuChiCaNhan.ViewModels;

public sealed class MonthOption(DateTime month)
{
    public DateTime StartDate { get; } = new(month.Year, month.Month, 1);
    public override string ToString() => StartDate.ToString("MMMM yyyy", System.Globalization.CultureInfo.GetCultureInfo("vi-VN"));
}
