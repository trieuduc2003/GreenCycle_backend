using System;
using System.Collections.Generic;

namespace GreenCycle.Domain.Entities;

public partial class Order
{
    public int OrderId { get; set; }

    public int SellerId { get; set; }

    public int MethodId { get; set; }

    public int StatusId { get; set; }

    public decimal? TotalEstimatedAmount { get; set; }

    public decimal? TotalActualAmount { get; set; }

    public decimal? PlatformFee { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual DropOffOrder? DropOffOrder { get; set; }

    public virtual ICollection<EscrowAccount> EscrowAccounts { get; set; } = new List<EscrowAccount>();

    public virtual OrderMethod Method { get; set; } = null!;

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual PickUpOrder? PickUpOrder { get; set; }

    public virtual User Seller { get; set; } = null!;

    public virtual OrderStatus Status { get; set; } = null!;

    public virtual ICollection<WalletTransaction> WalletTransactions { get; set; } = new List<WalletTransaction>();
}
