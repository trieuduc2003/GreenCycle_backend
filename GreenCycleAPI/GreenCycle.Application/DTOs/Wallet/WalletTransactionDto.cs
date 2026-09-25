using System;

namespace GreenCycle.Application.DTOs.Wallet
{
    /// <summary>DTO cho 1 giao dịch ví — dùng làm nguồn dữ liệu cho màn hình Thông báo.</summary>
    public class WalletTransactionDto
    {
        public int TransactionId { get; set; }
        public decimal Amount { get; set; }
        public string TransactionType { get; set; } = string.Empty;
        public int? ReferenceOrderId { get; set; }
        public int? ReferenceVoucherId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Description { get; set; }

        /// <summary>True nếu là giao dịch nhận tiền (Amount > 0)</summary>
        public bool IsCredit => Amount > 0;
    }
}
