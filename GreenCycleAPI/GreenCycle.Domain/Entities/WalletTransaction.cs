using System;
using System.Collections.Generic;

namespace GreenCycle.Domain.Entities;

public partial class WalletTransaction
{
    public int TransactionId { get; set; }

    public int WalletId { get; set; }

    public decimal Amount { get; set; }

    public string TransactionType { get; set; } = null!;

    public int? ReferenceOrderId { get; set; }

    public int? ReferenceVoucherId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? Description { get; set; }

    public virtual Order? ReferenceOrder { get; set; }

    public virtual UserVoucher? ReferenceVoucher { get; set; }

    public virtual Wallet Wallet { get; set; } = null!;
}
