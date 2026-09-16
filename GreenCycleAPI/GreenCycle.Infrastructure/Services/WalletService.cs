using GreenCycle.Application.DTOs.Wallet;
using GreenCycle.Application.Interfaces.Services;
using GreenCycle.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GreenCycle.Infrastructure.Services
{
    public class WalletService : IWalletService
    {
        private readonly GreenCycleDbContext _context;

        public WalletService(GreenCycleDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lấy số dư ví của người dùng theo userId.
        /// </summary>
        public async Task<WalletBalanceDto> GetBalanceAsync(int userId)
        {
            var wallet = await _context.Wallets
                .FirstOrDefaultAsync(w => w.UserId == userId)
                ?? throw new Exception("Không tìm thấy ví cho người dùng này.");

            return new WalletBalanceDto
            {
                WalletId    = wallet.WalletId,
                WalletType  = wallet.WalletType,
                Currency    = wallet.Currency,
                Balance     = wallet.Balance ?? 0,
                LastUpdated = wallet.LastUpdated,
            };
        }
    }
}
