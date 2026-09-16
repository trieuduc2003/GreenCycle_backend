using System;
using System.Collections.Generic;

namespace GreenCycle.Domain.Entities;

public partial class Voucher
{
    public int VoucherId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public decimal PointCost { get; set; }

    public decimal DiscountValue { get; set; }

    public int? Quantity { get; set; }

    public bool? IsActive { get; set; }

    public DateTime ExpiredAt { get; set; }

    public virtual ICollection<UserVoucher> UserVouchers { get; set; } = new List<UserVoucher>();
}
