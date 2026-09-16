using System;
using System.Collections.Generic;

namespace GreenCycle.Domain.Entities;

public partial class Payment
{
    public int PaymentId { get; set; }

    public int UserId { get; set; }

    public int? OrderId { get; set; }

    public decimal Amount { get; set; }

    public string PaymentMethod { get; set; } = null!;

    public string? ProviderTransactionId { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public virtual Order? Order { get; set; }

    public virtual User User { get; set; } = null!;
}
