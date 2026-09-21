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

        /// <summary>Lấy số dư ví của người dùng theo userId.</summary>
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

        /// <summary>Lấy lịch sử giao dịch ví (dùng làm nguồn thông báo).</summary>
        public async Task<List<WalletTransactionDto>> GetTransactionsAsync(int userId, int limit = 50)
        {
            var wallet = await _context.Wallets
                .FirstOrDefaultAsync(w => w.UserId == userId);

            if (wallet == null) return new List<WalletTransactionDto>();

            var transactions = await _context.WalletTransactions
                .Where(t => t.WalletId == wallet.WalletId)
                .OrderByDescending(t => t.CreatedAt)
                .Take(limit)
                .Select(t => new WalletTransactionDto
                {
                    TransactionId      = t.TransactionId,
                    Amount             = t.Amount,
                    TransactionType    = t.TransactionType ?? string.Empty,
                    ReferenceOrderId   = t.ReferenceOrderId,
                    ReferenceVoucherId = t.ReferenceVoucherId,
                    CreatedAt          = t.CreatedAt ?? DateTime.UtcNow,
                    Description        = t.Description,
                })
                .ToListAsync();

            return transactions;
        }
        public async Task<bool> DepositAsync(int userId, decimal amount, string description)
        {
            var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
            if (wallet == null) throw new Exception("Không tìm thấy ví cho người dùng này.");

            wallet.Balance = (wallet.Balance ?? 0) + amount;
            wallet.LastUpdated = DateTime.UtcNow;

            var transaction = new GreenCycle.Domain.Entities.WalletTransaction
            {
                WalletId = wallet.WalletId,
                Amount = amount,
                TransactionType = "Deposit",
                Description = description,
                CreatedAt = DateTime.UtcNow
            };

            _context.WalletTransactions.Add(transaction);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
