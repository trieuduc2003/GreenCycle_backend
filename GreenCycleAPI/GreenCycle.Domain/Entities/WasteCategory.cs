using System;
using System.Collections.Generic;

namespace GreenCycle.Domain.Entities;

public partial class WasteCategory
{
    public int CategoryId { get; set; }

    public string Name { get; set; } = null!;

    public string Unit { get; set; } = null!;

    public decimal? Co2ReductionFactor { get; set; }

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual ICollection<PricingMatrix> PricingMatrices { get; set; } = new List<PricingMatrix>();
}
