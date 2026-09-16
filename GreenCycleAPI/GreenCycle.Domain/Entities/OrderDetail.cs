using System;
using System.Collections.Generic;

namespace GreenCycle.Domain.Entities;

public partial class OrderDetail
{
    public int OrderDetailId { get; set; }

    public int OrderId { get; set; }

    public int CategoryId { get; set; }

    public decimal EstimatedWeight { get; set; }

    public decimal? ActualWeight { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal EstimatedSubTotal { get; set; }

    public decimal? ActualSubTotal { get; set; }

    public virtual WasteCategory Category { get; set; } = null!;

    public virtual Order Order { get; set; } = null!;
}
