using System;
using System.Collections.Generic;

namespace GreenCycle.Domain.Entities;

public partial class UserVoucher
{
    public int UserVoucherId { get; set; }

    public int UserId { get; set; }

    public int VoucherId { get; set; }

    public string VoucherCode { get; set; } = null!;

    public bool? IsUsed { get; set; }

    public DateTime? ReceivedAt { get; set; }

    public DateTime? UsedAt { get; set; }

    public virtual User User { get; set; } = null!;

    public virtual Voucher Voucher { get; set; } = null!;

    public virtual ICollection<WalletTransaction> WalletTransactions { get; set; } = new List<WalletTransaction>();
}
