using GreenCycle.Application.DTOs.Wallet;

namespace GreenCycle.Application.Interfaces.Services
{
    public interface IWalletService
    {
        /// <summary>Lấy số dư ví của người dùng theo userId.</summary>
        Task<WalletBalanceDto> GetBalanceAsync(int userId);

        /// <summary>Lấy lịch sử giao dịch ví (dùng làm nguồn thông báo).</summary>
        Task<List<WalletTransactionDto>> GetTransactionsAsync(int userId, int limit = 50);

        /// <summary>Nạp tiền vào ví.</summary>
        Task<bool> DepositAsync(int userId, decimal amount, string description);
    }
}

