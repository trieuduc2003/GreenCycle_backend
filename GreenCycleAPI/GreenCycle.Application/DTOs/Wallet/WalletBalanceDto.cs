using System;

namespace GreenCycle.Application.DTOs.Wallet
{
    public class WalletBalanceDto
    {
        public int WalletId { get; set; }
        public string WalletType { get; set; } = null!;
        public string Currency { get; set; } = null!;
        public decimal Balance { get; set; }
        public DateTime? LastUpdated { get; set; }

        /// <summary>
        /// Quy đổi sang VND (1 GP = 10 VND)
        /// </summary>
        public decimal BalanceInVnd => Balance * 10;
    }
}
