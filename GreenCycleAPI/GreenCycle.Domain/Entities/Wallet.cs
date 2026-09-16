using System;
using System.Collections.Generic;

namespace GreenCycle.Domain.Entities;

public partial class Wallet
{
    public int WalletId { get; set; }

    public int UserId { get; set; }

    public string WalletType { get; set; } = null!;

    public string Currency { get; set; } = null!;

    public decimal? Balance { get; set; }

    public DateTime? LastUpdated { get; set; }

    public virtual ICollection<EscrowAccount> EscrowAccounts { get; set; } = new List<EscrowAccount>();

    public virtual User User { get; set; } = null!;

    public virtual ICollection<WalletTransaction> WalletTransactions { get; set; } = new List<WalletTransaction>();
}
