using ThuChiCaNhan.Models;

namespace ThuChiCaNhan.ViewModels;

public sealed class WalletSummary
{
    public Wallet Wallet { get; init; } = null!;
    public decimal Balance { get; init; }
}
