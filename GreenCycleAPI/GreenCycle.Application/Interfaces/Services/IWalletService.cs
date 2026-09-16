using GreenCycle.Application.DTOs.Wallet;

namespace GreenCycle.Application.Interfaces.Services
{
    public interface IWalletService
    {
        /// <summary>
        /// Lấy số dư ví của người dùng theo userId.
        /// </summary>
        Task<WalletBalanceDto> GetBalanceAsync(int userId);
    }
}
