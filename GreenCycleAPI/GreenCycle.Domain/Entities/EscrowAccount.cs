using System;
using System.Collections.Generic;

namespace GreenCycle.Domain.Entities;

public partial class EscrowAccount
{
    public int EscrowId { get; set; }

    public int WalletId { get; set; }

    public decimal Amount { get; set; }

    public int? OrderId { get; set; }

    public string ReferenceType { get; set; } = null!;

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? Note { get; set; }

    public virtual Order? Order { get; set; }

    public virtual Wallet Wallet { get; set; } = null!;
}
